namespace Plugin.Maui.ApiCache.Tests;

public sealed class InvalidationTests
{
    [Fact]
    public async Task Invalidate_removes_a_single_key()
    {
        var (cache, _, _, _, _) = CacheHarness.Create();
        await cache.GetAsync<Customer>("/customers/1");

        Assert.True(await cache.ExistsAsync("/customers/1"));
        Assert.True(await cache.InvalidateAsync("/customers/1"));
        Assert.False(await cache.ExistsAsync("/customers/1"));
    }

    [Fact]
    public async Task InvalidateByPrefix_removes_matching_keys()
    {
        var (cache, _, _, _, _) = CacheHarness.Create();
        await cache.GetAsync<Customer>("/customers/1");
        await cache.GetAsync<Customer>("/customers/2");
        await cache.GetAsync<Customer>("/orders/1");

        var removed = await cache.InvalidateByPrefixAsync("/customers");

        Assert.Equal(2, removed);
        Assert.False(await cache.ExistsAsync("/customers/1"));
        Assert.True(await cache.ExistsAsync("/orders/1"));
    }

    [Fact]
    public async Task Clear_removes_everything()
    {
        var (cache, _, _, _, _) = CacheHarness.Create();
        await cache.GetAsync<Customer>("/customers/1");
        await cache.ClearAsync();

        Assert.False(await cache.ExistsAsync("/customers/1"));
    }

    [Fact]
    public async Task Events_fire_on_hit_and_miss()
    {
        var hits = 0;
        var misses = 0;
        var (cache, _, _, _, _) = CacheHarness.Create(options =>
        {
            options.Events.OnCacheHit = _ => hits++;
            options.Events.OnCacheMiss = _ => misses++;
        });

        await cache.GetAsync<Customer>("/customers/1", CachePolicy.CacheFirst);
        await cache.GetAsync<Customer>("/customers/1", CachePolicy.CacheFirst);

        Assert.Equal(1, misses);
        Assert.Equal(1, hits);
    }
}
