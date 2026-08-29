namespace Plugin.Maui.ApiCache.Tests;

public sealed class StoreTests
{
    [Fact]
    public async Task File_store_round_trips_and_survives_reload()
    {
        var storage = new FakeAppStorage();
        var options = new ApiCacheOptions { CacheDirectoryName = "store-test" };
        var monitor = new FakeOptionsMonitor<ApiCacheOptions>(options);
        var clock = new FakeClock();

        var first = new FileCacheStore(storage, monitor, clock);
        await first.SetAsync(new CacheRecord
        {
            Key = "/customers",
            Payload = "[1]",
            CachedAt = clock.UtcNow,
            ExpiresAt = clock.UtcNow + TimeSpan.FromMinutes(5)
        });
        first.Dispose();

        var second = new FileCacheStore(storage, monitor, clock);
        var loaded = await second.GetAsync("/customers");

        Assert.NotNull(loaded);
        Assert.Equal("[1]", loaded!.Payload);
        second.Dispose();
    }

    [Fact]
    public async Task Memory_store_evicts_oldest_when_over_max_entries()
    {
        var options = new ApiCacheOptions { MaxEntries = 2, PersistToDisk = false };
        var monitor = new FakeOptionsMonitor<ApiCacheOptions>(options);
        var clock = new FakeClock();
        var store = new MemoryCacheStore(monitor, clock);

        await store.SetAsync(new CacheRecord { Key = "a", Payload = "1", CachedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddMinutes(1) });
        clock.Advance(TimeSpan.FromSeconds(1));
        await store.SetAsync(new CacheRecord { Key = "b", Payload = "2", CachedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddMinutes(1) });
        clock.Advance(TimeSpan.FromSeconds(1));
        await store.SetAsync(new CacheRecord { Key = "c", Payload = "3", CachedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddMinutes(1) });

        Assert.False(await store.ExistsAsync("a"));
        Assert.True(await store.ExistsAsync("b"));
        Assert.True(await store.ExistsAsync("c"));
    }

    [Fact]
    public void Cache_key_normalizes_relative_paths()
    {
        var options = new ApiCacheOptions();
        Assert.Equal("/customers", CacheKeyBuilder.Build("customers", null, options));
        Assert.Equal("/customers", CacheKeyBuilder.Build("/customers", null, options));
    }

    [Fact]
    public void Cache_key_includes_vary_headers()
    {
        var options = new ApiCacheOptions();
        options.VaryHeaders.Add("Accept-Language");
        var key = CacheKeyBuilder.Build("/customers", new CacheRequestOptions
        {
            Headers = new Dictionary<string, string> { ["Accept-Language"] = "fr-FR" }
        }, options);

        Assert.Equal("/customers|Accept-Language=fr-FR", key);
    }
}
