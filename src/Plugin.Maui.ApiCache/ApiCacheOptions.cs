namespace Plugin.Maui.ApiCache;

/// <summary>
/// Root configuration for <c>Plugin.Maui.ApiCache</c>.
/// </summary>
public sealed class ApiCacheOptions
{
    /// <summary>
    /// Time-to-live used when a response does not carry <c>Cache-Control: max-age</c>.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = ApiCacheDefaults.DefaultExpiration;

    /// <summary>
    /// Policy used when a call does not specify one.
    /// </summary>
    public CachePolicy DefaultPolicy { get; set; } = CachePolicy.CacheFirst;

    /// <summary>
    /// Named <see cref="HttpClient"/> resolved from <see cref="IHttpClientFactory"/>.
    /// When empty, <see cref="ApiCacheDefaults.HttpClientName"/> is used.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// Base address applied to relative paths such as <c>/customers</c>.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Persist entries under the app-data directory. Set to <see langword="false"/>
    /// for an in-memory store (tests or session-only cache).
    /// </summary>
    public bool PersistToDisk { get; set; } = true;

    /// <summary>
    /// Folder name under the app-data directory.
    /// </summary>
    public string CacheDirectoryName { get; set; } = ApiCacheDefaults.CacheDirectoryName;

    /// <summary>
    /// Maximum stored payload bytes before least-recently-used eviction.
    /// </summary>
    public long MaxCacheSizeBytes { get; set; } = ApiCacheDefaults.MaxCacheSizeBytes;

    /// <summary>
    /// Maximum number of entries before least-recently-used eviction.
    /// </summary>
    public int MaxEntries { get; set; } = ApiCacheDefaults.MaxEntries;

    /// <summary>
    /// How long an expired entry may still be served under
    /// <see cref="CachePolicy.StaleWhileRevalidate"/>.
    /// </summary>
    public TimeSpan StaleWhileRevalidateWindow { get; set; } = ApiCacheDefaults.StaleWhileRevalidateWindow;

    /// <summary>
    /// Return a stale cache entry when <see cref="CachePolicy.CacheFirst"/>
    /// or <see cref="CachePolicy.NetworkFirst"/> cannot reach the network.
    /// </summary>
    public bool AllowStaleOnError { get; set; } = true;

    /// <summary>
    /// When using <see cref="CachePolicy.CacheOnly"/>, return expired entries
    /// instead of throwing <see cref="CacheMissException"/>.
    /// </summary>
    public bool AllowStaleOnCacheOnly { get; set; } = true;

    /// <summary>
    /// Only store responses with a 2xx status code.
    /// </summary>
    public bool CacheOnlySuccessfulResponses { get; set; } = true;

    /// <summary>
    /// Request header names that become part of the cache key (for example
    /// <c>Accept-Language</c>).
    /// </summary>
    public IList<string> VaryHeaders { get; } = new List<string>();

    /// <summary>
    /// JSON options used to serialize and deserialize typed payloads.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = JsonDefaults.Create();

    /// <summary>
    /// Optional override for connectivity. Return <see langword="false"/> to
    /// skip the network (pairs with NetworkMonitor).
    /// </summary>
    public Func<CancellationToken, ValueTask<bool>>? IsOnlineAsync { get; set; }

    /// <summary>
    /// Diagnostic callbacks.
    /// </summary>
    public ApiCacheEvents Events { get; set; } = new();
}
