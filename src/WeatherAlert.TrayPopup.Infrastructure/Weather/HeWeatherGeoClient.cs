using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Infrastructure.Weather;

public sealed class HeWeatherGeoClient : IGeoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<HeWeatherClientOptions> _options;
    private readonly ILogger<HeWeatherGeoClient> _logger;

    public HeWeatherGeoClient(
        HttpClient httpClient,
        IOptions<HeWeatherClientOptions> options,
        ILogger<HeWeatherGeoClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.Value.RequestTimeoutSeconds));
    }

    public Task<IReadOnlyList<GeoCity>> SearchCitiesInChinaAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult<IReadOnlyList<GeoCity>>(Array.Empty<GeoCity>());
        }

        return QueryAsync(
            location: keyword.Trim(),
            range: "cn",
            number: _options.Value.GeoSearchNumber,
            cancellationToken);
    }

    public Task<GeoCity?> LookupByCoordinatesAsync(double longitude, double latitude, CancellationToken cancellationToken)
    {
        var location = string.Create(
            CultureInfo.InvariantCulture,
            $"{longitude:F2},{latitude:F2}");
        return QueryFirstAsync(location, range: "cn", cancellationToken);
    }

    private async Task<GeoCity?> QueryFirstAsync(
        string location,
        string? range,
        CancellationToken cancellationToken)
    {
        var list = await QueryAsync(location, range, number: 1, cancellationToken).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : null;
    }

    private async Task<IReadOnlyList<GeoCity>> QueryAsync(
        string location,
        string? range,
        int number,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var query = new List<string>
        {
            $"location={Uri.EscapeDataString(location)}",
            $"key={Uri.EscapeDataString(options.ApiKey)}",
            $"number={Math.Clamp(number, 1, 20)}",
            "lang=zh"
        };
        if (!string.IsNullOrWhiteSpace(range))
        {
            query.Add($"range={Uri.EscapeDataString(range)}");
        }

        var endpoint = $"{options.GeoLookupEndpoint}?{string.Join('&', query)}";

        try
        {
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new WeatherApiException("Geo API authentication failed.", WeatherApiErrorKind.Authentication);
            }

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<GeoLookupResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (payload?.Code != "200" || payload.Locations is null)
            {
                _logger.LogWarning("Geo lookup returned code {Code} for location {Location}.", payload?.Code, location);
                return Array.Empty<GeoCity>();
            }

            return payload.Locations
                .Where(x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Name))
                .Select(MapLocation)
                .ToList();
        }
        catch (WeatherApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Geo lookup failed for location {Location}.", location);
            throw new WeatherApiException("Geo API request failed.", WeatherApiErrorKind.Network);
        }
    }

    private static GeoCity MapLocation(GeoLocationDto dto) =>
        new(
            dto.Id!,
            dto.Name!,
            dto.Adm1,
            dto.Adm2,
            dto.Country ?? "中国");

    private sealed class GeoLookupResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("location")]
        public List<GeoLocationDto>? Locations { get; set; }
    }

    private sealed class GeoLocationDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("adm1")]
        public string? Adm1 { get; set; }

        [JsonPropertyName("adm2")]
        public string? Adm2 { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}
