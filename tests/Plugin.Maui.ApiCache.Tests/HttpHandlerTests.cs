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
        Assert.Equal("false", second.Headers.GetValues("X-ApiCache-Stale").Single());
        Assert.Equal(nameof(CachePolicy.CacheFirst), second.Headers.GetValues("X-ApiCache-Policy").Single());
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Handler_serves_stale_when_offline()
    {
        var calls = 0;
        var clock = new FakeClock();
        var network = new FakeNetwork();
        var services = new ServiceCollection();
        services.AddSingleton<INetworkStatus>(network);
        services.AddSingleton<ISystemClock>(clock);
        services.AddApiCache(options =>
        {
            options.PersistToDisk = false;
            options.DefaultPolicy = CachePolicy.CacheFirst;
            options.DefaultExpiration = TimeSpan.FromMinutes(1);
            options.AllowStaleOnError = true;
        });
        services.AddHttpClient("demo", client => client.BaseAddress = new Uri("https://api.example.com/"))
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpHandler(_ =>
            {
                calls++;
                return CacheHarness.Json("{\"id\":1,\"name\":\"Ada\"}");
            }))
            .AddApiCache();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("demo");

        using var first = await client.GetAsync("/customers/1");
        clock.Advance(TimeSpan.FromHours(1));
        network.IsConnected = false;

        using var stale = await client.GetAsync("/customers/1");

        Assert.True(first.IsSuccessStatusCode);
        Assert.Equal("true", stale.Headers.GetValues("X-ApiCache-Hit").Single());
        Assert.Equal("true", stale.Headers.GetValues("X-ApiCache-Stale").Single());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void AddApiCache_resolves_IApiCache_when_HttpClient_is_registered()
    {
        var services = new ServiceCollection();
        services.AddApiCache(options =>
        {
            options.PersistToDisk = false;
            options.DefaultPolicy = CachePolicy.CacheOnly;
        });
        services.AddSingleton<INetworkStatus, FakeNetwork>();
        services.AddSingleton<ISystemClock, FakeClock>();
        services.AddHttpClient();

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IApiCache>();

        Assert.NotNull(cache);
    }
}
