using Microsoft.Extensions.Logging;
using Plugin.Maui.ApiCache;
using Plugin.Maui.ApiCache.Sample.Demo;

namespace Plugin.Maui.ApiCache.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.Services.AddSingleton<DemoNetwork>();
        builder.Services.AddSingleton<INetworkStatus>(sp => sp.GetRequiredService<DemoNetwork>());
        builder.Services.AddSingleton<DemoCatalogHandler>();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseApiCache(options =>
            {
                options.DefaultExpiration = TimeSpan.FromMinutes(30);
                options.DefaultPolicy = CachePolicy.CacheFirst;
                options.BaseAddress = new Uri("https://demo.local/");
                options.PersistToDisk = true;
            });

        builder.Services.AddHttpClient(ApiCacheDefaults.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://demo.local/");
        }).ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<DemoCatalogHandler>());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
