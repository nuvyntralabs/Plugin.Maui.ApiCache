namespace Plugin.Maui.ApiCache;

/// <summary>
/// Process-lifetime store used when <see cref="ApiCacheOptions.PersistToDisk"/> is false.
/// </summary>
public sealed class MemoryCacheStore : ICacheStore
{
    private readonly ConcurrentDictionary<string, CacheRecord> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAccess = new(StringComparer.Ordinal);
    private readonly IOptionsMonitor<ApiCacheOptions> _options;
    private readonly ISystemClock _clock;

    /// <summary>
    /// Creates an in-memory store.
    /// </summary>
    public MemoryCacheStore(IOptionsMonitor<ApiCacheOptions> options, ISystemClock? clock = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? new SystemClock();
    }

    /// <inheritdoc />
    public Task<CacheRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.TryGetValue(key, out var record);
        if (record is not null)
        {
            _lastAccess[key] = _clock.UtcNow;
        }

        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public Task SetAsync(CacheRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();
        _entries[record.Key] = record;
        _lastAccess[record.Key] = _clock.UtcNow;
        EvictIfNeeded();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _lastAccess.TryRemove(key, out _);
        return Task.FromResult(_entries.TryRemove(key, out _));
    }

    /// <inheritdoc />
    public Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var removed = 0;
        foreach (var key in _entries.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                if (_entries.TryRemove(key, out _))
                {
                    _lastAccess.TryRemove(key, out _);
                    removed++;
                }
            }
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Clear();
        _lastAccess.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_entries.ContainsKey(key));
    }

    private void EvictIfNeeded()
    {
        var options = _options.CurrentValue;
        var maxEntries = Math.Max(1, options.MaxEntries);
        var maxBytes = Math.Max(1, options.MaxCacheSizeBytes);

        while (_entries.Count > maxEntries || TotalBytes() > maxBytes)
        {
            var oldestKey = _lastAccess
                .OrderBy(pair => pair.Value)
                .Select(pair => pair.Key)
                .FirstOrDefault()
                ?? _entries.Keys.FirstOrDefault();

            if (oldestKey is null)
            {
                break;
            }

            _entries.TryRemove(oldestKey, out _);
            _lastAccess.TryRemove(oldestKey, out _);
        }
    }

    private long TotalBytes()
    {
        long total = 0;
        foreach (var entry in _entries.Values)
        {
            total += entry.SizeBytes;
        }

        return total;
    }
}
