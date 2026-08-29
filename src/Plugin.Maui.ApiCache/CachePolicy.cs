namespace Plugin.Maui.ApiCache;

/// <summary>
/// How <see cref="IApiCache"/> chooses between the local store and the network.
/// </summary>
public enum CachePolicy
{
    /// <summary>
    /// Return a fresh cache entry if one exists. Otherwise fetch the network,
    /// store the result, and return it. If the network fails and a stale entry
    /// exists, the stale value is returned when allowed.
    /// </summary>
    CacheFirst = 0,

    /// <summary>
    /// Try the network first. On success, update the cache. On failure, fall
    /// back to any cached value (fresh or stale).
    /// </summary>
    NetworkFirst = 1,

    /// <summary>
    /// Return a cached value immediately (fresh or stale within the revalidate
    /// window) and refresh the store in the background. Waits for the network
    /// when nothing is cached.
    /// </summary>
    StaleWhileRevalidate = 2,

    /// <summary>
    /// Always call the network. Successful responses are written through to the
    /// cache. The store is never read.
    /// </summary>
    NetworkOnly = 3,

    /// <summary>
    /// Read only from the cache. Throws <see cref="CacheMissException"/> when
    /// the key is missing or unusable.
    /// </summary>
    CacheOnly = 4
}
