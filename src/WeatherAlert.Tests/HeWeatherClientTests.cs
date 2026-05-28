using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.Infrastructure.Weather;
using Xunit;

namespace WeatherAlert.Tests;

public sealed class HeWeatherClientTests
{
    [Fact]
    public async Task GetHourlyForecastAsync_ValidPayload_ReturnsMappedItems()
    {
        const string payload = """
        {
          "code": "200",
          "hourly": [
            { "fxTime": "2026-05-28T11:00+08:00", "precip": "0.5", "pop": "60", "text": "Rain" },
            { "fxTime": "2026-05-29T08:00+08:00", "precip": "0", "pop": "0", "text": "Cloudy" }
          ]
        }
        """;

        using var client = new HttpClient(new StubHandler(payload))
        {
            BaseAddress = new Uri("https://example.com")
        };

        var apiClient = new HeWeatherClient(
            client,
            Options.Create(new HeWeatherClientOptions
            {
                ApiKey = "demo-key",
                WeatherEndpoint = "/v7/weather/24h",
                RequestTimeoutSeconds = 5
            }),
            NullLogger<HeWeatherClient>.Instance);

        var result = await apiClient.GetHourlyForecastAsync("101010100", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(0.5, result[0].PrecipitationMm, 3);
        Assert.Equal(60, result[0].PrecipitationProbability);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _payload;

        public StubHandler(string payload)
        {
            _payload = payload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
