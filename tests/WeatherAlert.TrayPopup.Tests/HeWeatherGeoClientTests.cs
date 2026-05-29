using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.Infrastructure.Weather;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class HeWeatherGeoClientTests
{
    [Fact]
    public async Task SearchCitiesInChinaAsync_ValidPayload_ReturnsMappedCities()
    {
        const string payload = """
        {
          "code": "200",
          "location": [
            { "name": "杭州", "id": "101210101", "adm1": "浙江省", "adm2": "杭州", "country": "中国" },
            { "name": "海宁", "id": "101210301", "adm1": "浙江省", "adm2": "嘉兴", "country": "中国" }
          ]
        }
        """;

        var client = CreateClient(payload);
        var results = await client.SearchCitiesInChinaAsync("杭州", CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("101210101", results[0].Id);
        Assert.Contains("杭州", results[0].DisplayName);
    }

    [Fact]
    public async Task LookupByCoordinatesAsync_ValidPayload_ReturnsNearestCity()
    {
        const string payload = """
        {
          "code": "200",
          "location": [
            { "name": "北京", "id": "101010100", "adm1": "北京市", "adm2": "北京", "country": "中国" }
          ]
        }
        """;

        var client = CreateClient(payload);
        var city = await client.LookupByCoordinatesAsync(116.41, 39.92, CancellationToken.None);

        Assert.NotNull(city);
        Assert.Equal("101010100", city!.Id);
        Assert.Equal("北京", city.Name);
    }

    [Fact]
    public async Task SearchCitiesInChinaAsync_EmptyKeyword_ReturnsEmpty()
    {
        var client = CreateClient("{}");
        var results = await client.SearchCitiesInChinaAsync("  ", CancellationToken.None);
        Assert.Empty(results);
    }

    private static HeWeatherGeoClient CreateClient(string payload)
    {
        var handler = new StubHandler(payload);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        return new HeWeatherGeoClient(
            http,
            Options.Create(new HeWeatherClientOptions
            {
                ApiKey = "demo",
                GeoLookupEndpoint = "/geo/v2/city/lookup",
                GeoSearchNumber = 20
            }),
            NullLogger<HeWeatherGeoClient>.Instance);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _payload;

        public StubHandler(string payload) => _payload = payload;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;
            Assert.Contains("range=cn", uri, StringComparison.Ordinal);
            Assert.Contains("lang=zh", uri, StringComparison.Ordinal);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            });
        }
    }
}
