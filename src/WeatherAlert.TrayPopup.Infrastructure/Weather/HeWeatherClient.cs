using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Infrastructure.Weather;

public sealed class HeWeatherClient : IWeatherApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<HeWeatherClientOptions> _options;
    private readonly ILogger<HeWeatherClient> _logger;

    public HeWeatherClient(
        HttpClient httpClient,
        IOptions<HeWeatherClientOptions> options,
        ILogger<HeWeatherClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.Value.RequestTimeoutSeconds));
    }

    public async Task<IReadOnlyList<HourlyForecast>> GetHourlyForecastAsync(
        string cityCode,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var endpoint = $"{options.WeatherEndpoint}?location={Uri.EscapeDataString(cityCode)}&key={Uri.EscapeDataString(options.ApiKey)}";
        var delays = RetryPolicyFactory.CreateExponentialBackoff(3);

        for (var attempt = 1; attempt <= delays.Count; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw new WeatherApiException("Weather API authentication failed.", WeatherApiErrorKind.Authentication);
                }

                if ((int)response.StatusCode >= 500)
                {
                    throw new WeatherApiException("Weather API server error.", WeatherApiErrorKind.ServerError);
                }

                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<HeWeatherResponse>(cancellationToken: cancellationToken);
                if (payload?.Code != "200")
                {
                    throw new WeatherApiException($"Weather API business code: {payload?.Code ?? "unknown"}", WeatherApiErrorKind.BadResponse);
                }

                var mapped = payload.Hourly?
                    .Select(MapHourly)
                    .OrderBy(x => x.ForecastTime)
                    .ToList() ?? new List<HourlyForecast>();

                return mapped;
            }
            catch (WeatherApiException)
            {
                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Weather API timeout on attempt {Attempt}.", attempt);
                if (attempt == delays.Count)
                {
                    throw new WeatherApiException("Weather API timeout after retries.", WeatherApiErrorKind.Timeout);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Weather API request failed on attempt {Attempt}.", attempt);
                if (attempt == delays.Count)
                {
                    throw new WeatherApiException("Weather API network error after retries.", WeatherApiErrorKind.Network);
                }
            }

            await Task.Delay(delays[attempt - 1], cancellationToken);
        }

        throw new WeatherApiException("Weather API request failed unexpectedly.", WeatherApiErrorKind.Unknown);
    }

    private static HourlyForecast MapHourly(HeWeatherHourlyDto dto)
    {
        _ = DateTimeOffset.TryParse(dto.FxTime, out var forecastTime);
        _ = double.TryParse(dto.Precip, out var precipitationMm);
        _ = int.TryParse(dto.Pop, out var pop);

        return new HourlyForecast(
            forecastTime,
            precipitationMm,
            pop,
            dto.Text);
    }

    private sealed class HeWeatherResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("hourly")]
        public List<HeWeatherHourlyDto>? Hourly { get; set; }
    }

    private sealed class HeWeatherHourlyDto
    {
        [JsonPropertyName("fxTime")]
        public string? FxTime { get; set; }

        [JsonPropertyName("precip")]
        public string? Precip { get; set; }

        [JsonPropertyName("pop")]
        public string? Pop { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
