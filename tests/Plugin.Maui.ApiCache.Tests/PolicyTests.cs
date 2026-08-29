namespace Plugin.Maui.ApiCache.Tests;

public sealed class PolicyTests
{
    [Fact]
    public async Task CacheFirst_returns_store_on_fresh_hit()
    {
        var (cache, http, _, _, _) = CacheHarness.Create();

        var first = await cache.GetAsync<Customer>("/customers/1", CachePolicy.CacheFirst);
        var second = await cache.GetAsync<Customer>("/customers/1", CachePolicy.CacheFirst);

        Assert.Equal("Ada", first?.Name);
        Assert.Equal("Ada", second?.Name);
        Assert.Equal(1, http.SendCount);
    }

    [Fact]
    public async Task CacheFirst_falls_back_to_stale_when_offline()
    {
        var (cache, _, clock, network, _) = CacheHarness.Create();
        await cache.GetAsync<Customer>("/customers/1", CachePolicy.CacheFirst);

        clock.Advance(TimeSpan.FromHours(1));
        network.IsConnected = false;

        var result = await cache.GetResultAsync<Customer>("/customers/1", new CacheRequestOptions
        {
            Policy = CachePolicy.CacheFirst
        });

        Assert.True(result.FromCache);
        Assert.True(result.IsStale);
        Assert.Equal("Ada", result.Value?.Name);
    }

    [Fact]
    public async Task NetworkFirst_prefers_network_then_updates_cache()
    {
        var calls = 0;
        var (cache, _, _, _, _) = CacheHarness.Create(respond: _ =>
        {
            calls++;
            return CacheHarness.Json($"{{\"id\":1,\"name\":\"Ada-{calls}\"}}");
        });

        var first = await cache.GetAsync<Customer>("/customers/1", CachePolicy.NetworkFirst);
        var second = await cache.GetAsync<Customer>("/customers/1", CachePolicy.NetworkFirst);

        Assert.Equal("Ada-1", first?.Name);
        Assert.Equal("Ada-2", second?.Name);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task NetworkFirst_uses_cache_when_offline()
    {
        var (cache, _, _, network, _) = CacheHarness.Create();
        await cache.GetAsync<Customer>("/customers/1", CachePolicy.NetworkFirst);

        network.IsConnected = false;
        var result = await cache.GetResultAsync<Customer>("/customers/1", new CacheRequestOptions
        {
            Policy = CachePolicy.NetworkFirst
        });

        Assert.True(result.FromCache);
        Assert.Equal("Ada", result.Value?.Name);
    }

    [Fact]
    public async Task StaleWhileRevalidate_returns_cache_and_refreshes()
    {
        var calls = 0;
        var (cache, _, clock, _, _) = CacheHarness.Create(respond: _ =>
        {
            calls++;
            return CacheHarness.Json($"{{\"id\":1,\"name\":\"Ada-{calls}\"}}");
        });

        await cache.GetAsync<Customer>("/customers/1", CachePolicy.NetworkOnly);
        clock.Advance(TimeSpan.FromHours(1));

        var stale = await cache.GetResultAsync<Customer>("/customers/1", new CacheRequestOptions
        {
            Policy = CachePolicy.StaleWhileRevalidate
        });

        Assert.True(stale.FromCache);
        Assert.True(stale.IsStale);
        Assert.Equal("Ada-1", stale.Value?.Name);

        await WaitForAsync(() => calls >= 2);
        var refreshed = await cache.GetResultAsync<Customer>("/customers/1", new CacheRequestOptions
        {
            Policy = CachePolicy.CacheOnly
        });
        Assert.Equal("Ada-2", refreshed.Value?.Name);
    }

    [Fact]
    public async Task NetworkOnly_ignores_cache()
    {
        var calls = 0;
        var (cache, _, _, _, _) = CacheHarness.Create(respond: _ =>
        {
            calls++;
            return CacheHarness.Json($"{{\"id\":1,\"name\":\"Ada-{calls}\"}}");
        });

        await cache.GetAsync<Customer>("/customers/1", CachePolicy.NetworkOnly);
        await cache.GetAsync<Customer>("/customers/1", CachePolicy.NetworkOnly);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task CacheOnly_throws_on_miss()
    {
        var (cache, _, _, _, _) = CacheHarness.Create();

        await Assert.ThrowsAsync<CacheMissException>(
            () => cache.GetAsync<Customer>("/missing", CachePolicy.CacheOnly));
    }

    [Fact]
    public async Task CacheOnly_returns_warmed_value()
    {
        var (cache, http, _, _, _) = CacheHarness.Create();
        await cache.SetAsync("/customers/1", new Customer { Id = 1, Name = "Grace" });

        var value = await cache.GetAsync<Customer>("/customers/1", CachePolicy.CacheOnly);

        Assert.Equal("Grace", value?.Name);
        Assert.Equal(0, http.SendCount);
    }

    [Fact]
    public async Task GetResultAsync_reports_metadata()
    {
        var (cache, _, clock, _, _) = CacheHarness.Create();

        var result = await cache.GetResultAsync<Customer>("/customers/1");

        Assert.False(result.FromCache);
        Assert.Equal(CachePolicy.CacheFirst, result.Policy);
        Assert.Equal("/customers/1", result.Key);
        Assert.Equal(clock.UtcNow, result.CachedAt);
        Assert.Equal(clock.UtcNow + TimeSpan.FromMinutes(30), result.ExpiresAt);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(condition(), "Timed out waiting for background revalidation.");
    }
}
