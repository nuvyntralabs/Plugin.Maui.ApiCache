namespace Plugin.Maui.ApiCache;

/// <summary>
/// Thrown by <see cref="CachePolicy.CacheOnly"/> when no usable entry exists.
/// </summary>
public sealed class CacheMissException : ApiCacheException
{
    /// <summary>
    /// Creates a miss exception for <paramref name="key"/>.
    /// </summary>
    public CacheMissException(string key)
        : base($"No usable cache entry exists for '{key}'.")
    {
        Key = key;
    }

    /// <summary>Key that missed.</summary>
    public string Key { get; }
}
