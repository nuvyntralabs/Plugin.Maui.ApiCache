using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.ApiCache.Tests;

public sealed class HttpHandlerTests
{
    [Fact]
    public async Task Handler_caches_get_on_cache_first()
    {
        var calls = 0;
        var services = new ServiceCollection();
        services.AddApiCache(options =>
        {
            options.PersistToDisk = false;
            options.DefaultPolicy = CachePolicy.CacheFirst;
            options.DefaultExpiration = TimeSpan.FromMinutes(10);
        });
        services.AddSingleton<INetworkStatus, FakeNetwork>();
        services.AddSingleton<ISystemClock, FakeClock>();
        services.AddHttpClient("demo", client => client.BaseAddress = new Uri("https://api.example.com/"))
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpHandler(_ =>
            {
                calls++;
                return CacheHarness.Json("{\"id\":1,\"name\":\"Ada\"}");
            }))
            .AddApiCache();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("demo");

        using var first = await client.GetAsync("/customers/1");
        using var second = await client.GetAsync("/customers/1");

        Assert.True(first.IsSuccessStatusCode);
        Assert.True(second.Headers.Contains("X-ApiCache-Hit"));
        Assert.Equal("true", second.Headers.GetValues("X-ApiCache-Hit").Single());
        Assert.Equal(1, calls);
    }
}
