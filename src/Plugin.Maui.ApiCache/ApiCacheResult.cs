namespace Plugin.Maui.ApiCache;

/// <summary>
/// Typed payload plus cache metadata for a single <see cref="IApiCache"/> call.
/// </summary>
/// <typeparam name="T">Deserialized response type.</typeparam>
public sealed class ApiCacheResult<T>
{
    /// <summary>
    /// Creates a result.
    /// </summary>
    public ApiCacheResult(
        T? value,
        bool fromCache,
        bool isStale,
        CachePolicy policy,
        string key,
        DateTimeOffset? cachedAt,
        DateTimeOffset? expiresAt)
    {
        Value = value;
        FromCache = fromCache;
        IsStale = isStale;
        Policy = policy;
        Key = key;
        CachedAt = cachedAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>Deserialized body. May be default when the payload is empty.</summary>
    public T? Value { get; }

    /// <summary>True when the value was read from the store.</summary>
    public bool FromCache { get; }

    /// <summary>True when the stored value was past its expiration.</summary>
    public bool IsStale { get; }

    /// <summary>Policy that produced this result.</summary>
    public CachePolicy Policy { get; }

    /// <summary>Resolved cache key.</summary>
    public string Key { get; }

    /// <summary>When the entry was last written.</summary>
    public DateTimeOffset? CachedAt { get; }

    /// <summary>When the entry is considered stale.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>True when a value was produced.</summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue => Value is not null;
}
