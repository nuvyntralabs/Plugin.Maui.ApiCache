namespace Plugin.Maui.ApiCache;

/// <summary>
/// Durable or in-memory store for serialized cache records.
/// </summary>
public interface ICacheStore
{
    /// <summary>
    /// Load the record for <paramref name="key"/>, or <see langword="null"/> when missing.
    /// </summary>
    Task<CacheRecord?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert or replace <paramref name="record"/>.
    /// </summary>
    Task SetAsync(CacheRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove <paramref name="key"/>.
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove keys that start with <paramref name="prefix"/>.
    /// </summary>
    Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove every entry.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a record exists for <paramref name="key"/>.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
