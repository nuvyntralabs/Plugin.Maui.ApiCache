namespace Plugin.Maui.ApiCache;

/// <summary>
/// Shared names and defaults for <c>Plugin.Maui.ApiCache</c>.
/// </summary>
public static class ApiCacheDefaults
{
    /// <summary>
    /// Named <see cref="HttpClient"/> registered by <c>UseApiCache</c>.
    /// </summary>
    public const string HttpClientName = "ApiCache";

    /// <summary>
    /// Folder under the app-data directory that holds cache files.
    /// </summary>
    public const string CacheDirectoryName = "apicache";

    /// <summary>
    /// Default time-to-live when the response has no <c>Cache-Control</c> max-age.
    /// </summary>
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How long an expired entry may still be served under <see cref="CachePolicy.StaleWhileRevalidate"/>.
    /// </summary>
    public static readonly TimeSpan StaleWhileRevalidateWindow = TimeSpan.FromHours(24);

    /// <summary>
    /// Soft cap on stored payload bytes (LRU eviction).
    /// </summary>
    public const long MaxCacheSizeBytes = 20L * 1024 * 1024;

    /// <summary>
    /// Soft cap on stored entries (LRU eviction).
    /// </summary>
    public const int MaxEntries = 500;
}
