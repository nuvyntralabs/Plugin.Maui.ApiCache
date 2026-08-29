namespace Plugin.Maui.ApiCache;

/// <summary>
/// Base exception for the API cache pipeline.
/// </summary>
public class ApiCacheException : Exception
{
    /// <summary>
    /// Creates an exception.
    /// </summary>
    public ApiCacheException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with an inner cause.
    /// </summary>
    public ApiCacheException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
