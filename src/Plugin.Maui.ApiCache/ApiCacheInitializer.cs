using Microsoft.Maui.Hosting;

namespace Plugin.Maui.ApiCache;

/// <summary>
/// Publishes the registered <see cref="IApiCache"/> on <see cref="ApiCache.Default"/>.
/// </summary>
internal sealed class ApiCacheInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var cache = services.GetService<IApiCache>();
        if (cache is not null)
        {
            ApiCache.SetDefault(cache);
        }
    }
}
