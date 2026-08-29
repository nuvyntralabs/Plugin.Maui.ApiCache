namespace Plugin.Maui.ApiCache;

/// <summary>
/// Thrown when the network is required and no cache fallback is available.
/// </summary>
public sealed class CacheNetworkException : ApiCacheException
{
    /// <summary>
    /// Creates a network exception.
    /// </summary>
    public CacheNetworkException(string key, string message, Exception? innerException = null)
        : base(message, innerException ?? new HttpRequestException(message))
    {
        Key = key;
    }

    /// <summary>Key that was being fetched.</summary>
    public string Key { get; }
}
