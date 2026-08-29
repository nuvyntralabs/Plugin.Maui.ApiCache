namespace Plugin.Maui.ApiCache;

/// <summary>
/// Default <see cref="IApiCache"/> implementation.
/// </summary>
public sealed class ApiCacheClient : IApiCache
{
    private readonly ICacheStore _store;
    private readonly IOptionsMonitor<ApiCacheOptions> _options;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly HttpClient? _httpClient;
    private readonly INetworkStatus _network;
    private readonly ISystemClock _clock;
    private readonly ILogger<ApiCacheClient> _logger;
    private readonly ConcurrentDictionary<string, Lazy<Task<CacheRecord>>> _inflight = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates the cache client used by DI.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ApiCacheClient(
        ICacheStore store,
        IOptionsMonitor<ApiCacheOptions> options,
        INetworkStatus network,
        ISystemClock clock,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<ApiCacheClient>? logger = null)
        : this(store, options, network, clock, httpClientFactory, httpClient: null, logger)
    {
    }

    /// <summary>
    /// Creates the cache client with an explicit <see cref="HttpClient"/> (tests / no host).
    /// </summary>
    public ApiCacheClient(
        ICacheStore store,
        IOptionsMonitor<ApiCacheOptions> options,
        INetworkStatus network,
        ISystemClock clock,
        HttpClient httpClient,
        ILogger<ApiCacheClient>? logger = null)
        : this(store, options, network, clock, httpClientFactory: null, httpClient, logger)
    {
    }

    private ApiCacheClient(
        ICacheStore store,
        IOptionsMonitor<ApiCacheOptions> options,
        INetworkStatus network,
        ISystemClock clock,
        IHttpClientFactory? httpClientFactory,
        HttpClient? httpClient,
        ILogger<ApiCacheClient>? logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<ApiCacheClient>.Instance;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        => GetAsync<T>(path, options: null, cancellationToken);

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string path, CachePolicy policy, CancellationToken cancellationToken = default)
        => GetAsync<T>(path, new CacheRequestOptions { Policy = policy }, cancellationToken);

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string path, CacheRequestOptions? options, CancellationToken cancellationToken = default)
    {
        var result = await GetResultAsync<T>(path, options, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    /// <inheritdoc />
    public async Task<ApiCacheResult<T>> GetResultAsync<T>(
        string path,
        CacheRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var settings = _options.CurrentValue;
        var request = options ?? new CacheRequestOptions();
        var policy = request.Policy ?? settings.DefaultPolicy;
        var key = CacheKeyBuilder.Build(path, request, settings);
        var now = _clock.UtcNow;
        var cached = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);

        return policy switch
        {
            CachePolicy.CacheOnly => await CacheOnlyAsync<T>(key, cached, request, settings, now, policy).ConfigureAwait(false),
            CachePolicy.NetworkOnly => await NetworkOnlyAsync<T>(path, key, request, settings, policy, cancellationToken).ConfigureAwait(false),
            CachePolicy.CacheFirst => await CacheFirstAsync<T>(path, key, cached, request, settings, now, policy, cancellationToken).ConfigureAwait(false),
            CachePolicy.NetworkFirst => await NetworkFirstAsync<T>(path, key, cached, request, settings, now, policy, cancellationToken).ConfigureAwait(false),
            CachePolicy.StaleWhileRevalidate => await StaleWhileRevalidateAsync<T>(path, key, cached, request, settings, now, policy, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown cache policy.")
        };
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        key = CacheKeyBuilder.NormalizePath(key);
        var settings = _options.CurrentValue;
        var ttl = expiration ?? settings.DefaultExpiration;
        var now = _clock.UtcNow;
        var payload = JsonSerializer.Serialize(value, settings.JsonSerializerOptions);
        await _store.SetAsync(new CacheRecord
        {
            Key = key,
            Payload = payload,
            CachedAt = now,
            ExpiresAt = now + ttl,
            StatusCode = 200,
            ContentType = "application/json"
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _store.RemoveAsync(CacheKeyBuilder.NormalizePath(key), cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> InvalidateByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        return _store.RemoveByPrefixAsync(CacheKeyBuilder.NormalizePath(prefix), cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => _store.ClearAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _store.ExistsAsync(CacheKeyBuilder.NormalizePath(key), cancellationToken);
    }

    private Task<ApiCacheResult<T>> CacheOnlyAsync<T>(
        string key,
        CacheRecord? cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        DateTimeOffset now,
        CachePolicy policy)
    {
        var allowStale = request.AllowStale ?? settings.AllowStaleOnCacheOnly;
        if (cached is null || (!cached.IsFresh(now) && !allowStale))
        {
            Raise(settings.Events.OnCacheMiss, new ApiCacheEvent(key, policy, false, false));
            throw new CacheMissException(key);
        }

        var stale = cached.IsExpired(now);
        Raise(stale ? settings.Events.OnStaleServed : settings.Events.OnCacheHit,
            new ApiCacheEvent(key, policy, true, stale));
        return Task.FromResult(ToResult<T>(cached, fromCache: true, stale, policy, request, settings));
    }

    private async Task<ApiCacheResult<T>> NetworkOnlyAsync<T>(
        string path,
        string key,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        var record = await FetchAndStoreAsync(path, key, cached: null, request, settings, policy, cancellationToken)
            .ConfigureAwait(false);
        return ToResult<T>(record, fromCache: false, isStale: false, policy, request, settings);
    }

    private async Task<ApiCacheResult<T>> CacheFirstAsync<T>(
        string path,
        string key,
        CacheRecord? cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        DateTimeOffset now,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        if (cached is not null && cached.IsFresh(now))
        {
            Raise(settings.Events.OnCacheHit, new ApiCacheEvent(key, policy, true, false));
            return ToResult<T>(cached, fromCache: true, isStale: false, policy, request, settings);
        }

        Raise(settings.Events.OnCacheMiss, new ApiCacheEvent(key, policy, false, cached is not null));

        try
        {
            var record = await FetchAndStoreAsync(path, key, cached, request, settings, policy, cancellationToken)
                .ConfigureAwait(false);
            return ToResult<T>(record, fromCache: false, isStale: false, policy, request, settings);
        }
        catch (Exception ex) when (ShouldFallback(request, settings, cached))
        {
            Raise(settings.Events.OnStaleServed, new ApiCacheEvent(key, policy, true, true, ex));
            return ToResult<T>(cached!, fromCache: true, isStale: true, policy, request, settings);
        }
    }

    private async Task<ApiCacheResult<T>> NetworkFirstAsync<T>(
        string path,
        string key,
        CacheRecord? cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        DateTimeOffset now,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await FetchAndStoreAsync(path, key, cached, request, settings, policy, cancellationToken)
                .ConfigureAwait(false);
            return ToResult<T>(record, fromCache: false, isStale: false, policy, request, settings);
        }
        catch (Exception ex) when (ShouldFallback(request, settings, cached))
        {
            var stale = cached!.IsExpired(now);
            Raise(stale ? settings.Events.OnStaleServed : settings.Events.OnCacheHit,
                new ApiCacheEvent(key, policy, true, stale, ex));
            return ToResult<T>(cached, fromCache: true, isStale: stale, policy, request, settings);
        }
    }

    private async Task<ApiCacheResult<T>> StaleWhileRevalidateAsync<T>(
        string path,
        string key,
        CacheRecord? cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        DateTimeOffset now,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        var window = request.StaleWhileRevalidateWindow ?? settings.StaleWhileRevalidateWindow;
        if (cached is not null && cached.CanServeStale(now, window))
        {
            var stale = cached.IsExpired(now);
            Raise(stale ? settings.Events.OnStaleServed : settings.Events.OnCacheHit,
                new ApiCacheEvent(key, policy, true, stale));

            if (stale)
            {
                _ = RevalidateInBackground(path, key, cached, request, settings, policy);
            }

            return ToResult<T>(cached, fromCache: true, stale, policy, request, settings);
        }

        Raise(settings.Events.OnCacheMiss, new ApiCacheEvent(key, policy, false, false));
        var record = await FetchAndStoreAsync(path, key, cached, request, settings, policy, cancellationToken)
            .ConfigureAwait(false);
        return ToResult<T>(record, fromCache: false, isStale: false, policy, request, settings);
    }

    private async Task RevalidateInBackground(
        string path,
        string key,
        CacheRecord cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        CachePolicy policy)
    {
        try
        {
            await FetchAndStoreAsync(path, key, cached, request, settings, policy, CancellationToken.None)
                .ConfigureAwait(false);
            Raise(settings.Events.OnRevalidated, new ApiCacheEvent(key, policy, false, false));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Background revalidation failed for {Key}.", key);
            Raise(settings.Events.OnNetworkFailed, new ApiCacheEvent(key, policy, true, true, ex));
        }
    }

    private async Task<CacheRecord> FetchAndStoreAsync(
        string path,
        string key,
        CacheRecord? cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        if (!await IsOnlineAsync(settings, cancellationToken).ConfigureAwait(false))
        {
            var offline = new CacheNetworkException(key, $"Device is offline; cannot fetch '{key}'.");
            Raise(settings.Events.OnNetworkFailed, new ApiCacheEvent(key, policy, false, false, offline));
            throw offline;
        }

        var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<CacheRecord>>(
            () => SendAsync(path, key, cached, request, settings, policy, cancellationToken),
            LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazy.Value.ConfigureAwait(false);
        }
        finally
        {
            _inflight.TryRemove(key, out _);
        }
    }

    private async Task<CacheRecord> SendAsync(
        string path,
        string key,
        CacheRecord? cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, ResolveUri(path, settings));
        if (request.Headers is not null)
        {
            foreach (var header in request.Headers)
            {
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(cached?.ETag))
        {
            message.Headers.TryAddWithoutValidation("If-None-Match", cached.ETag);
        }

        HttpResponseMessage response;
        try
        {
            response = await GetClient().SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            Raise(settings.Events.OnNetworkFailed, new ApiCacheEvent(key, policy, false, false, ex));
            throw new CacheNetworkException(key, $"Network request failed for '{key}'.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotModified && cached is not null)
            {
                var refreshed = CloneWithNewExpiry(cached, request, settings, response);
                await _store.SetAsync(refreshed, cancellationToken).ConfigureAwait(false);
                Raise(settings.Events.OnRevalidated, new ApiCacheEvent(key, policy, true, false));
                return refreshed;
            }

            if (!response.IsSuccessStatusCode && settings.CacheOnlySuccessfulResponses)
            {
                var status = new CacheNetworkException(
                    key,
                    $"HTTP {(int)response.StatusCode} from '{key}' was not cached.");
                Raise(settings.Events.OnNetworkFailed, new ApiCacheEvent(key, policy, false, false, status));
                throw status;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var now = _clock.UtcNow;
            var ttl = ResolveTtl(request, settings, response);
            var record = new CacheRecord
            {
                Key = key,
                Payload = payload,
                CachedAt = now,
                ExpiresAt = now + ttl,
                ETag = response.Headers.ETag?.Tag,
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json"
            };

            await _store.SetAsync(record, cancellationToken).ConfigureAwait(false);
            Raise(settings.Events.OnNetworkStored, new ApiCacheEvent(key, policy, false, false));
            return record;
        }
    }

    private HttpClient GetClient()
    {
        if (_httpClient is not null)
        {
            return _httpClient;
        }

        if (_httpClientFactory is not null)
        {
            var name = string.IsNullOrWhiteSpace(_options.CurrentValue.HttpClientName)
                ? ApiCacheDefaults.HttpClientName
                : _options.CurrentValue.HttpClientName;
            return _httpClientFactory.CreateClient(name);
        }

        throw new InvalidOperationException(
            "No HttpClient is configured. Register AddHttpClient or pass an HttpClient to ApiCacheClient.");
    }

    private Uri ResolveUri(string path, ApiCacheOptions settings)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        var relative = path.StartsWith('/') ? path : "/" + path;
        if (settings.BaseAddress is not null)
        {
            return new Uri(settings.BaseAddress, relative);
        }

        var client = GetClient();
        if (client.BaseAddress is not null)
        {
            return new Uri(client.BaseAddress, relative);
        }

        throw new ApiCacheException(
            $"Cannot resolve '{path}'. Set ApiCacheOptions.BaseAddress or the HttpClient BaseAddress.");
    }

    private async ValueTask<bool> IsOnlineAsync(ApiCacheOptions settings, CancellationToken cancellationToken)
    {
        if (settings.IsOnlineAsync is not null)
        {
            return await settings.IsOnlineAsync(cancellationToken).ConfigureAwait(false);
        }

        return _network.IsConnected;
    }

    private static bool ShouldFallback(CacheRequestOptions request, ApiCacheOptions settings, CacheRecord? cached)
        => cached is not null && (request.AllowStaleOnError ?? settings.AllowStaleOnError);

    private CacheRecord CloneWithNewExpiry(
        CacheRecord cached,
        CacheRequestOptions request,
        ApiCacheOptions settings,
        HttpResponseMessage response)
    {
        var now = _clock.UtcNow;
        return new CacheRecord
        {
            Key = cached.Key,
            Payload = cached.Payload,
            CachedAt = now,
            ExpiresAt = now + ResolveTtl(request, settings, response),
            ETag = response.Headers.ETag?.Tag ?? cached.ETag,
            StatusCode = cached.StatusCode,
            ContentType = cached.ContentType
        };
    }

    private static TimeSpan ResolveTtl(
        CacheRequestOptions request,
        ApiCacheOptions settings,
        HttpResponseMessage response)
    {
        if (request.Expiration is { } explicitTtl)
        {
            return explicitTtl;
        }

        if (response.Headers.CacheControl?.MaxAge is { } maxAge && maxAge > TimeSpan.Zero)
        {
            return maxAge;
        }

        return settings.DefaultExpiration;
    }

    private ApiCacheResult<T> ToResult<T>(
        CacheRecord record,
        bool fromCache,
        bool isStale,
        CachePolicy policy,
        CacheRequestOptions request,
        ApiCacheOptions settings)
    {
        var value = Deserialize<T>(record.Payload, request, settings);
        return new ApiCacheResult<T>(value, fromCache, isStale, policy, record.Key, record.CachedAt, record.ExpiresAt);
    }

    private static T? Deserialize<T>(string payload, CacheRequestOptions request, ApiCacheOptions settings)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        if (request.JsonTypeInfo is JsonTypeInfo<T> typed)
        {
            return JsonSerializer.Deserialize(payload, typed);
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)payload;
        }

        var options = request.JsonSerializerOptions ?? settings.JsonSerializerOptions;
        return JsonSerializer.Deserialize<T>(payload, options);
    }

    private static void Raise(Action<ApiCacheEvent>? callback, ApiCacheEvent evt)
    {
        try
        {
            callback?.Invoke(evt);
        }
        catch
        {
            // Event handlers must not break the cache pipeline.
        }
    }
}
