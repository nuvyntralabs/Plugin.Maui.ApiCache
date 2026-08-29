namespace Plugin.Maui.ApiCache;

/// <summary>
/// Attaches <see cref="ApiCacheHandler"/> to an <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds GET-response caching to this client. Typed <see cref="IApiCache"/>
    /// calls should use a client that does <em>not</em> also add this handler,
    /// to avoid double caching.
    /// </summary>
    public static IHttpClientBuilder AddApiCache(
        this IHttpClientBuilder builder,
        Action<ApiCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddApiCache(configure);
        builder.AddHttpMessageHandler(sp => new ApiCacheHandler(
            sp.GetRequiredService<ICacheStore>(),
            sp.GetRequiredService<IOptionsMonitor<ApiCacheOptions>>(),
            sp.GetRequiredService<INetworkStatus>(),
            sp.GetRequiredService<ISystemClock>()));

        return builder;
    }
}
