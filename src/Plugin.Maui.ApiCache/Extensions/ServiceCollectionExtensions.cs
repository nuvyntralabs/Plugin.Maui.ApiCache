namespace Plugin.Maui.ApiCache;

/// <summary>
/// Registers the API cache and its default store.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IApiCache"/>, the on-device store, and a named <see cref="HttpClient"/>.
    /// </summary>
    public static IServiceCollection AddApiCache(this IServiceCollection services, Action<ApiCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ApiCacheOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient(ApiCacheDefaults.HttpClientName);
        services.TryAddSingleton<IAppStorage, MauiAppStorage>();
        services.TryAddSingleton<INetworkStatus, MauiNetworkStatus>();
        services.TryAddSingleton<ISystemClock, SystemClock>();
        services.TryAddSingleton<ICacheStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<ApiCacheOptions>>();
            return options.CurrentValue.PersistToDisk
                ? new FileCacheStore(
                    sp.GetRequiredService<IAppStorage>(),
                    options,
                    sp.GetRequiredService<ISystemClock>(),
                    sp.GetService<ILogger<FileCacheStore>>())
                : new MemoryCacheStore(options, sp.GetRequiredService<ISystemClock>());
        });
        services.TryAddSingleton<IApiCache, ApiCacheClient>();

        return services;
    }
}
