using Microsoft.Maui.Networking;

namespace Plugin.Maui.ApiCache;

/// <summary>
/// <see cref="INetworkStatus"/> backed by MAUI <see cref="Connectivity"/>.
/// </summary>
public sealed class MauiNetworkStatus : INetworkStatus
{
    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            try
            {
                return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
