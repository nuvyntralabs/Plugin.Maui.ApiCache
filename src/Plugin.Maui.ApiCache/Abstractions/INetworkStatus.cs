namespace Plugin.Maui.ApiCache;

/// <summary>
/// Connectivity probe used to skip the network when the device is offline.
/// </summary>
public interface INetworkStatus
{
    /// <summary>
    /// True when a network attempt is worth making.
    /// </summary>
    bool IsConnected { get; }
}
