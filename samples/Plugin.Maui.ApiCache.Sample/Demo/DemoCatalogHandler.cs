using System.Net;
using System.Text;

namespace Plugin.Maui.ApiCache.Sample.Demo;

public sealed class DemoCatalogHandler : HttpMessageHandler
{
    private int _generation = 1;

    public int FetchCount { get; private set; }

    public void BumpGeneration() => _generation++;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        FetchCount++;
        var body = $$"""
            {"id":1,"name":"Ada Lovelace v{{_generation}}","city":"London"}
            """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"v{_generation}\"");
        return Task.FromResult(response);
    }
}
