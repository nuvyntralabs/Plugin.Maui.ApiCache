namespace Plugin.Maui.ApiCache;

/// <summary>
/// <see cref="DelegatingHandler"/> that applies <see cref="CachePolicy"/> to GET requests
/// without going back through <see cref="IApiCache"/> (avoids handler recursion).
/// </summary>
public sealed class ApiCacheHandler : DelegatingHandler
{
    private readonly ICacheStore _store;
    private readonly IOptionsMonitor<ApiCacheOptions> _options;
    private readonly INetworkStatus _network;
    private readonly ISystemClock _clock;
    private readonly ConcurrentDictionary<string, Lazy<Task<CacheRecord>>> _inflight = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates the handler.
    /// </summary>
    public ApiCacheHandler(
        ICacheStore store,
        IOptionsMonitor<ApiCacheOptions> options,
        INetworkStatus network,
        ISystemClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Method != HttpMethod.Get || request.RequestUri is null)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var settings = _options.CurrentValue;
        var path = request.RequestUri.IsAbsoluteUri
            ? request.RequestUri.GetLeftPart(UriPartial.Query)
            : request.RequestUri.OriginalString;
        var key = CacheKeyBuilder.Build(path, null, settings);
        var policy = settings.DefaultPolicy;
        var now = _clock.UtcNow;
        var cached = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);

        switch (policy)
        {
            case CachePolicy.CacheOnly:
                if (cached is null || (!cached.IsFresh(now) && !settings.AllowStaleOnCacheOnly))
                {
                    throw new CacheMissException(key);
                }

                return ToResponse(request, cached, fromCache: true, cached.IsExpired(now), policy);

            case CachePolicy.CacheFirst:
                if (cached is not null && cached.IsFresh(now))
                {
                    return ToResponse(request, cached, fromCache: true, isStale: false, policy);
                }

                try
                {
                    var fresh = await FetchAsync(request, key, cached, settings, cancellationToken).ConfigureAwait(false);
                    return ToResponse(request, fresh, fromCache: false, isStale: false, policy);
                }
                catch (Exception) when (cached is not null && settings.AllowStaleOnError)
                {
                    return ToResponse(request, cached, fromCache: true, isStale: true, policy);
                }

            case CachePolicy.NetworkFirst:
                try
                {
                    var fresh = await FetchAsync(request, key, cached, settings, cancellationToken).ConfigureAwait(false);
                    return ToResponse(request, fresh, fromCache: false, isStale: false, policy);
                }
                catch (Exception) when (cached is not null && settings.AllowStaleOnError)
                {
                    return ToResponse(request, cached, fromCache: true, isStale: cached.IsExpired(now), policy);
                }

            case CachePolicy.StaleWhileRevalidate:
                var window = settings.StaleWhileRevalidateWindow;
                if (cached is not null && cached.CanServeStale(now, window))
                {
                    if (cached.IsExpired(now))
                    {
                        _ = RevalidateAsync(request, key, cached, settings);
                    }

                    return ToResponse(request, cached, fromCache: true, cached.IsExpired(now), policy);
                }

                var loaded = await FetchAsync(request, key, cached, settings, cancellationToken).ConfigureAwait(false);
                return ToResponse(request, loaded, fromCache: false, isStale: false, policy);

            case CachePolicy.NetworkOnly:
            default:
                var network = await FetchAsync(request, key, cached, settings, cancellationToken).ConfigureAwait(false);
                return ToResponse(request, network, fromCache: false, isStale: false, policy);
        }
    }

    private async Task RevalidateAsync(HttpRequestMessage original, string key, CacheRecord cached, ApiCacheOptions settings)
    {
        try
        {
            using var clone = CloneGet(original);
            await FetchAsync(clone, key, cached, settings, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Background refresh is best-effort.
        }
    }

    private async Task<CacheRecord> FetchAsync(
        HttpRequestMessage request,
        string key,
        CacheRecord? cached,
        ApiCacheOptions settings,
        CancellationToken cancellationToken)
    {
        if (!await IsOnlineAsync(settings, cancellationToken).ConfigureAwait(false))
        {
            throw new CacheNetworkException(key, $"Device is offline; cannot fetch '{key}'.");
        }

        var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<CacheRecord>>(
            () => SendAndStoreAsync(request, key, cached, settings, CancellationToken.None),
            LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazy.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (lazy.Value.IsCompleted)
            {
                _inflight.TryRemove(key, out _);
            }
        }
    }

    private async Task<CacheRecord> SendAndStoreAsync(
        HttpRequestMessage request,
        string key,
        CacheRecord? cached,
        ApiCacheOptions settings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(cached?.ETag))
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", cached.ETag);
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            throw new CacheNetworkException(key, $"Network request failed for '{key}'.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotModified && cached is not null)
            {
                var now = _clock.UtcNow;
                var refreshed = new CacheRecord
                {
                    Key = cached.Key,
                    Payload = cached.Payload,
                    CachedAt = now,
                    ExpiresAt = now + ResolveTtl(settings, response),
                    ETag = response.Headers.ETag?.Tag ?? cached.ETag,
                    StatusCode = cached.StatusCode,
                    ContentType = cached.ContentType
                };
                await _store.SetAsync(refreshed, cancellationToken).ConfigureAwait(false);
                return refreshed;
            }

            if (!response.IsSuccessStatusCode && settings.CacheOnlySuccessfulResponses)
            {
                throw new CacheNetworkException(key, $"HTTP {(int)response.StatusCode} from '{key}' was not cached.");
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var writtenAt = _clock.UtcNow;
            var record = new CacheRecord
            {
                Key = key,
                Payload = payload,
                CachedAt = writtenAt,
                ExpiresAt = writtenAt + ResolveTtl(settings, response),
                ETag = response.Headers.ETag?.Tag,
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json"
            };
            await _store.SetAsync(record, cancellationToken).ConfigureAwait(false);
            return record;
        }
    }

    private async ValueTask<bool> IsOnlineAsync(ApiCacheOptions settings, CancellationToken cancellationToken)
    {
        if (settings.IsOnlineAsync is not null)
        {
            return await settings.IsOnlineAsync(cancellationToken).ConfigureAwait(false);
        }

        return _network.IsConnected;
    }

    private static TimeSpan ResolveTtl(ApiCacheOptions settings, HttpResponseMessage response)
    {
        if (response.Headers.CacheControl?.MaxAge is { } maxAge && maxAge > TimeSpan.Zero)
        {
            return maxAge;
        }

        return settings.DefaultExpiration;
    }

    private static HttpResponseMessage ToResponse(
        HttpRequestMessage request,
        CacheRecord record,
        bool fromCache,
        bool isStale,
        CachePolicy policy)
    {
        var response = new HttpResponseMessage((HttpStatusCode)record.StatusCode)
        {
            Content = new StringContent(record.Payload, Encoding.UTF8, record.ContentType ?? "application/json"),
            RequestMessage = request
        };
        response.Headers.TryAddWithoutValidation("X-ApiCache-Hit", fromCache ? "true" : "false");
        response.Headers.TryAddWithoutValidation("X-ApiCache-Stale", isStale ? "true" : "false");
        response.Headers.TryAddWithoutValidation("X-ApiCache-Policy", policy.ToString());
        if (!string.IsNullOrWhiteSpace(record.ETag))
        {
            response.Headers.TryAddWithoutValidation("ETag", record.ETag);
        }

        return response;
    }

    private static HttpRequestMessage CloneGet(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(HttpMethod.Get, original.RequestUri);
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
