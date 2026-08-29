namespace Plugin.Maui.ApiCache;

/// <summary>
/// <see cref="ISystemClock"/> backed by <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
