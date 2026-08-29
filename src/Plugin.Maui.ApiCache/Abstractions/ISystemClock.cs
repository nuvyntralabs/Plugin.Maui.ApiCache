namespace Plugin.Maui.ApiCache;

/// <summary>
/// Clock abstraction so tests can expire entries without waiting.
/// </summary>
public interface ISystemClock
{
    /// <summary>Current UTC timestamp.</summary>
    DateTimeOffset UtcNow { get; }
}
