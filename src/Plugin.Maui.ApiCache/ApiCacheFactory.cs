namespace Plugin.Maui.ApiCache;

/// <summary>
/// Builds an <see cref="IApiCache"/> without the generic host.
/// </summary>
public static class ApiCacheFactory
{
    /// <summary>
    /// Creates a cache that uses <paramref name="httpClient"/> for network fetches.
    /// </summary>
    public static IApiCache Create(HttpClient httpClient, Action<ApiCacheOptions>? configure = null, IAppStorage? storage = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        var options = new ApiCacheOptions();
        configure?.Invoke(options);
        var monitor = new StaticOptionsMonitor<ApiCacheOptions>(options);
        var clock = new SystemClock();
        ICacheStore store = options.PersistToDisk
            ? new FileCacheStore(storage ?? new MauiAppStorage(), monitor, clock)
            : new MemoryCacheStore(monitor, clock);

        return new ApiCacheClient(store, monitor, new MauiNetworkStatus(), clock, httpClient);
    }
}
