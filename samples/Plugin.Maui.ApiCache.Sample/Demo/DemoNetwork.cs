namespace Plugin.Maui.ApiCache.Sample.Demo;

public sealed class DemoNetwork : INetworkStatus
{
    public bool ForceOffline { get; set; }

    public bool IsConnected => !ForceOffline;
}
