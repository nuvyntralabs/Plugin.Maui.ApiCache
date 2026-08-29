namespace Plugin.Maui.ApiCache;

/// <summary>
/// Serialized cache entry stored on disk or in memory.
/// </summary>
public sealed class CacheRecord
{
    /// <summary>Cache key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Raw JSON (or text) payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>When the entry was last written.</summary>
    public DateTimeOffset CachedAt { get; set; }

    /// <summary>When the entry becomes stale.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Optional ETag from the origin.</summary>
    public string? ETag { get; set; }

    /// <summary>HTTP status stored with the payload.</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>Content type of the payload.</summary>
    public string? ContentType { get; set; } = "application/json";

    /// <summary>Payload size in bytes (UTF-8).</summary>
    [JsonIgnore]
    public int SizeBytes => Encoding.UTF8.GetByteCount(Payload);

    /// <summary>True when <paramref name="now"/> is at or past <see cref="ExpiresAt"/>.</summary>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>True when the entry is still within its TTL.</summary>
    public bool IsFresh(DateTimeOffset now) => now < ExpiresAt;

    /// <summary>
    /// True when the entry may still be served as stale under the given window.
    /// </summary>
    public bool CanServeStale(DateTimeOffset now, TimeSpan window)
    {
        if (window == Timeout.InfiniteTimeSpan || window == TimeSpan.MaxValue)
        {
            return true;
        }

        if (window <= TimeSpan.Zero)
        {
            return IsFresh(now);
        }

        return now < ExpiresAt + window;
    }
}
