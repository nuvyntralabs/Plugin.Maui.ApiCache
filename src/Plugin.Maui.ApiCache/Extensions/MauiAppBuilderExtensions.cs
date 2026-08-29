using Microsoft.Maui.Hosting;

namespace Plugin.Maui.ApiCache;

/// <summary>
/// MAUI host registration for the API cache.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IApiCache"/> on the MAUI host.
    /// Equivalent to <see cref="ServiceCollectionExtensions.AddApiCache"/>.
    /// </summary>
    public static MauiAppBuilder UseApiCache(this MauiAppBuilder builder, Action<ApiCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddApiCache(configure);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMauiInitializeService, ApiCacheInitializer>());
        return builder;
    }
}
