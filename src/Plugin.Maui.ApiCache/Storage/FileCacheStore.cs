namespace Plugin.Maui.ApiCache;

/// <summary>
/// JSON file store under the app-data directory.
/// </summary>
public sealed class FileCacheStore : ICacheStore, IDisposable
{
    private readonly IAppStorage _storage;
    private readonly IOptionsMonitor<ApiCacheOptions> _options;
    private readonly ISystemClock _clock;
    private readonly ILogger<FileCacheStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _directory;
    private readonly string _indexPath;
    private CacheIndex? _index;

    /// <summary>
    /// Creates the file store.
    /// </summary>
    public FileCacheStore(
        IAppStorage storage,
        IOptionsMonitor<ApiCacheOptions> options,
        ISystemClock? clock = null,
        ILogger<FileCacheStore>? logger = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? new SystemClock();
        _logger = logger ?? NullLogger<FileCacheStore>.Instance;
        _directory = Path.Combine(_storage.AppDataDirectory, _options.CurrentValue.CacheDirectoryName);
        Directory.CreateDirectory(_directory);
        _indexPath = Path.Combine(_directory, "index.json");
    }

    /// <inheritdoc />
    public async Task<CacheRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var entry = index.Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal));
            if (entry is null)
            {
                return null;
            }

            var path = Path.Combine(_directory, entry.FileName);
            if (!File.Exists(path))
            {
                index.Entries.Remove(entry);
                await SaveIndexUnlockedAsync(index, cancellationToken).ConfigureAwait(false);
                return null;
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var record = JsonSerializer.Deserialize(json, CacheJsonContext.Default.CacheRecord);
            entry.LastAccessUtc = _clock.UtcNow;
            await SaveIndexUnlockedAsync(index, cancellationToken).ConfigureAwait(false);
            return record;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(CacheRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var fileName = ToFileName(record.Key);
            var path = Path.Combine(_directory, fileName);
            var json = JsonSerializer.Serialize(record, CacheJsonContext.Default.CacheRecord);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);

            var existing = index.Entries.FirstOrDefault(e => string.Equals(e.Key, record.Key, StringComparison.Ordinal));
            if (existing is null)
            {
                existing = new CacheIndexEntry { Key = record.Key, FileName = fileName };
                index.Entries.Add(existing);
            }

            existing.FileName = fileName;
            existing.SizeBytes = Encoding.UTF8.GetByteCount(json);
            existing.LastAccessUtc = _clock.UtcNow;
            EvictUnlocked(index);
            await SaveIndexUnlockedAsync(index, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var removed = RemoveUnlocked(index, key);
            if (removed)
            {
                await SaveIndexUnlockedAsync(index, cancellationToken).ConfigureAwait(false);
            }

            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var keys = index.Entries
                .Where(e => e.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(e => e.Key)
                .ToArray();

            var removed = 0;
            foreach (var key in keys)
            {
                if (RemoveUnlocked(index, key))
                {
                    removed++;
                }
            }

            if (removed > 0)
            {
                await SaveIndexUnlockedAsync(index, cancellationToken).ConfigureAwait(false);
            }

            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Directory.Exists(_directory))
            {
                foreach (var file in Directory.EnumerateFiles(_directory))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to delete cache file {File}.", file);
                    }
                }
            }

            _index = new CacheIndex();
            await SaveIndexUnlockedAsync(_index, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexUnlockedAsync(cancellationToken).ConfigureAwait(false);
            return index.Entries.Any(e => string.Equals(e.Key, key, StringComparison.Ordinal));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task<CacheIndex> LoadIndexUnlockedAsync(CancellationToken cancellationToken)
    {
        if (_index is not null)
        {
            return _index;
        }

        if (!File.Exists(_indexPath))
        {
            _index = new CacheIndex();
            return _index;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_indexPath, cancellationToken).ConfigureAwait(false);
            _index = JsonSerializer.Deserialize(json, CacheJsonContext.Default.CacheIndex) ?? new CacheIndex();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache index was unreadable and will be rebuilt.");
            _index = new CacheIndex();
        }

        return _index;
    }

    private async Task SaveIndexUnlockedAsync(CacheIndex index, CancellationToken cancellationToken)
    {
        _index = index;
        Directory.CreateDirectory(_directory);
        var json = JsonSerializer.Serialize(index, CacheJsonContext.Default.CacheIndex);
        await File.WriteAllTextAsync(_indexPath, json, cancellationToken).ConfigureAwait(false);
    }

    private bool RemoveUnlocked(CacheIndex index, string key)
    {
        var entry = index.Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal));
        if (entry is null)
        {
            return false;
        }

        index.Entries.Remove(entry);
        var path = Path.Combine(_directory, entry.FileName);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to delete cache file for {Key}.", key);
            }
        }

        return true;
    }

    private void EvictUnlocked(CacheIndex index)
    {
        var options = _options.CurrentValue;
        var maxEntries = Math.Max(1, options.MaxEntries);
        var maxBytes = Math.Max(1, options.MaxCacheSizeBytes);

        while (index.Entries.Count > maxEntries || index.Entries.Sum(e => e.SizeBytes) > maxBytes)
        {
            var oldest = index.Entries.OrderBy(e => e.LastAccessUtc).FirstOrDefault();
            if (oldest is null)
            {
                break;
            }

            RemoveUnlocked(index, oldest.Key);
        }
    }

    internal static string ToFileName(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash).ToLowerInvariant() + ".json";
    }
}
