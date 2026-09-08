using Plugin.Maui.ApiCache;
using Plugin.Maui.ApiCache.Sample.Demo;

namespace Plugin.Maui.ApiCache.Sample;

public partial class MainPage : ContentPage
{
    private readonly IApiCache _cache;
    private readonly DemoNetwork _network;
    private readonly DemoCatalogHandler _origin;

    public MainPage(IApiCache cache, DemoNetwork network, DemoCatalogHandler origin)
    {
        InitializeComponent();
        _cache = cache;
        _network = network;
        _origin = origin;
    }

    private void OnCacheFirstClicked(object? sender, EventArgs e) => _ = RunAsync(CachePolicy.CacheFirst);

    private void OnNetworkFirstClicked(object? sender, EventArgs e) => _ = RunAsync(CachePolicy.NetworkFirst);

    private void OnSwrClicked(object? sender, EventArgs e) => _ = RunAsync(CachePolicy.StaleWhileRevalidate);

    private void OnNetworkOnlyClicked(object? sender, EventArgs e) => _ = RunAsync(CachePolicy.NetworkOnly);

    private void OnCacheOnlyClicked(object? sender, EventArgs e) => _ = RunAsync(CachePolicy.CacheOnly);

    private void OnToggleOfflineClicked(object? sender, EventArgs e)
    {
        _network.ForceOffline = !_network.ForceOffline;
        OfflineToggleBtn.Text = _network.ForceOffline ? "Simulate offline: ON" : "Simulate offline: OFF";
        StatusLabel.Text = _network.IsConnected ? "Online." : "Offline. CacheFirst / NetworkFirst will serve stale.";
    }

    private async void OnSetClicked(object? sender, EventArgs e)
    {
        await _cache.SetAsync("/customers/1", new Customer { Id = 1, Name = "Grace Hopper", City = "Arlington" });
        StatusLabel.Text = "Warmed /customers/1 with SetAsync (Grace Hopper). CacheOnly will read it without a fetch.";
    }

    private async void OnExistsClicked(object? sender, EventArgs e)
    {
        var exists = await _cache.ExistsAsync("/customers/1");
        StatusLabel.Text = $"Exists /customers/1: {exists}. Origin fetches: {_origin.FetchCount}.";
    }

    private async void OnInvalidateKeyClicked(object? sender, EventArgs e)
    {
        var removed = await _cache.InvalidateAsync("/customers/1");
        StatusLabel.Text = $"Invalidate /customers/1: {(removed ? "removed" : "missing")}. Origin fetches: {_origin.FetchCount}.";
    }

    private async void OnInvalidateClicked(object? sender, EventArgs e)
    {
        var removed = await _cache.InvalidateByPrefixAsync("/customers");
        StatusLabel.Text = $"Invalidated {removed} customer key(s). Origin fetches: {_origin.FetchCount}.";
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        await _cache.ClearAsync();
        StatusLabel.Text = $"Cleared cache. Origin fetches: {_origin.FetchCount}.";
    }

    private void OnBumpClicked(object? sender, EventArgs e)
    {
        _origin.BumpGeneration();
        StatusLabel.Text = "Origin generation bumped. Next network fetch returns a new name.";
    }

    private async Task RunAsync(CachePolicy policy)
    {
        try
        {
            var result = await _cache.GetResultAsync<Customer>("/customers/1", new CacheRequestOptions
            {
                Policy = policy
            });

            StatusLabel.Text =
                $"{policy}: {(result.FromCache ? "cache" : "network")}{(result.IsStale ? " (stale)" : string.Empty)}" +
                $"{Environment.NewLine}{result.Value?.Name} — {result.Value?.City}" +
                $"{Environment.NewLine}Origin fetches: {_origin.FetchCount}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{policy} failed: {ex.Message}{Environment.NewLine}Origin fetches: {_origin.FetchCount}";
        }
    }
}
