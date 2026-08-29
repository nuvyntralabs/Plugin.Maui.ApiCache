namespace Plugin.Maui.ApiCache;

/// <summary>
/// Lightweight HTTP/API cache with explicit fetch policies.
/// </summary>
public interface IApiCache
{
    /// <summary>
    /// GET <paramref name="path"/> using the default policy and deserialize as <typeparamref name="T"/>.
    /// </summary>
    Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// GET <paramref name="path"/> using <paramref name="policy"/>.
    /// </summary>
    Task<T?> GetAsync<T>(string path, CachePolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// GET <paramref name="path"/> with per-call options.
    /// </summary>
    Task<T?> GetAsync<T>(string path, CacheRequestOptions? options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="GetAsync{T}(string, CacheRequestOptions?, CancellationToken)"/>
    /// but includes cache metadata.
    /// </summary>
    Task<ApiCacheResult<T>> GetResultAsync<T>(string path, CacheRequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write <paramref name="value"/> into the store under <paramref name="key"/>.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a single key.
    /// </summary>
    /// <returns><see langword="true"/> when an entry was removed.</returns>
    Task<bool> InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove every key that starts with <paramref name="prefix"/> (ordinal, case-sensitive).
    /// </summary>
    Task<int> InvalidateByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove every entry.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a (possibly stale) entry exists for <paramref name="key"/>.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
