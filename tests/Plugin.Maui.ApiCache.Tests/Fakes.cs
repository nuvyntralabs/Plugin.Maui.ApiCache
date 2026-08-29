using Microsoft.Extensions.Options;

namespace Plugin.Maui.ApiCache.Tests;

internal sealed class FakeClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    public void Advance(TimeSpan delta) => UtcNow += delta;
}

internal sealed class FakeNetwork : INetworkStatus
{
    public bool IsConnected { get; set; } = true;
}

internal sealed class FakeAppStorage : IAppStorage
{
    public FakeAppStorage()
    {
        AppDataDirectory = Path.Combine(Path.GetTempPath(), "ApiCacheTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(AppDataDirectory);
    }

    public string AppDataDirectory { get; }
}

internal sealed class FakeOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    public FakeOptionsMonitor(T value) => CurrentValue = value;

    public T CurrentValue { get; set; }

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => _respond = respond;

    public int SendCount { get; private set; }

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SendCount++;
        Requests.Add(request);
        return Task.FromResult(_respond(request));
    }
}

internal sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

internal static class CacheHarness
{
    public static (ApiCacheClient Cache, FakeHttpHandler Http, FakeClock Clock, FakeNetwork Network, MemoryCacheStore Store) Create(
        Action<ApiCacheOptions>? configure = null,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var options = new ApiCacheOptions
        {
            PersistToDisk = false,
            DefaultExpiration = TimeSpan.FromMinutes(30),
            BaseAddress = new Uri("https://api.example.com/")
        };
        configure?.Invoke(options);

        var clock = new FakeClock();
        var network = new FakeNetwork();
        var monitor = new FakeOptionsMonitor<ApiCacheOptions>(options);
        var store = new MemoryCacheStore(monitor, clock);
        var handler = new FakeHttpHandler(respond ?? (_ => Json("{\"id\":1,\"name\":\"Ada\"}")));
        var client = new HttpClient(handler) { BaseAddress = options.BaseAddress };
        var cache = new ApiCacheClient(store, monitor, network, clock, client);
        return (cache, handler, clock, network, store);
    }

    public static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK, string? etag = null)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        if (etag is not null)
        {
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(etag);
        }

        return response;
    }
}
