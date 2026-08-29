namespace Plugin.Maui.ApiCache;

/// <summary>
/// Per-call overrides for <see cref="IApiCache.GetAsync{T}(string, CacheRequestOptions?, CancellationToken)"/>.
/// </summary>
public sealed class CacheRequestOptions
{
    /// <summary>
    /// Policy for this call. When omitted, <see cref="ApiCacheOptions.DefaultPolicy"/> is used.
    /// </summary>
    public CachePolicy? Policy { get; set; }

    /// <summary>
    /// Time-to-live written for a successful network response.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Stale window for <see cref="CachePolicy.StaleWhileRevalidate"/> on this call.
    /// </summary>
    public TimeSpan? StaleWhileRevalidateWindow { get; set; }

    /// <summary>
    /// Explicit cache key. When omitted, the path (and vary headers) are used.
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Extra request headers sent on the network fetch.
    /// </summary>
    public IDictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Return stale cache when the network fails. Defaults to
    /// <see cref="ApiCacheOptions.AllowStaleOnError"/>.
    /// </summary>
    public bool? AllowStaleOnError { get; set; }

    /// <summary>
    /// Return expired cache under <see cref="CachePolicy.CacheOnly"/>.
    /// Defaults to <see cref="ApiCacheOptions.AllowStaleOnCacheOnly"/>.
    /// </summary>
    public bool? AllowStale { get; set; }

    /// <summary>
    /// JSON options for this call.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Source-generated type info for AOT-safe deserialization.
    /// </summary>
    public JsonTypeInfo? JsonTypeInfo { get; set; }
}
