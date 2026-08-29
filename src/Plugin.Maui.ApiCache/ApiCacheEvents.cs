namespace Plugin.Maui.ApiCache;

/// <summary>
/// Diagnostic callbacks raised by the cache pipeline.
/// </summary>
public sealed class ApiCacheEvents
{
    /// <summary>
    /// A usable cache entry was returned without waiting for the network.
    /// </summary>
    public Action<ApiCacheEvent>? OnCacheHit { get; set; }

    /// <summary>
    /// No usable cache entry was found for the key.
    /// </summary>
    public Action<ApiCacheEvent>? OnCacheMiss { get; set; }

    /// <summary>
    /// A stale entry was returned (SWR or error fallback).
    /// </summary>
    public Action<ApiCacheEvent>? OnStaleServed { get; set; }

    /// <summary>
    /// A background or foreground revalidation completed.
    /// </summary>
    public Action<ApiCacheEvent>? OnRevalidated { get; set; }

    /// <summary>
    /// The network was used and the response was stored.
    /// </summary>
    public Action<ApiCacheEvent>? OnNetworkStored { get; set; }

    /// <summary>
    /// The network failed. <see cref="ApiCacheEvent.Exception"/> may be set.
    /// </summary>
    public Action<ApiCacheEvent>? OnNetworkFailed { get; set; }
}

/// <summary>
/// Payload for <see cref="ApiCacheEvents"/>.
/// </summary>
public sealed class ApiCacheEvent
{
    /// <summary>
    /// Creates an event.
    /// </summary>
    public ApiCacheEvent(
        string key,
        CachePolicy policy,
        bool fromCache,
        bool isStale,
        Exception? exception = null)
    {
        Key = key;
        Policy = policy;
        FromCache = fromCache;
        IsStale = isStale;
        Exception = exception;
    }

    /// <summary>Cache key.</summary>
    public string Key { get; }

    /// <summary>Policy that produced this event.</summary>
    public CachePolicy Policy { get; }

    /// <summary>Whether the value came from the store.</summary>
    public bool FromCache { get; }

    /// <summary>Whether the stored value was past its expiration.</summary>
    public bool IsStale { get; }

    /// <summary>Set when a network attempt failed.</summary>
    public Exception? Exception { get; }
}
