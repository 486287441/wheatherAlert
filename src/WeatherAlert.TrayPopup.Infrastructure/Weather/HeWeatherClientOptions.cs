namespace WeatherAlert.TrayPopup.Infrastructure.Weather;

public sealed class HeWeatherClientOptions
{
    public const string SectionName = "Weather";

    public string ApiKey { get; set; } = string.Empty;

    public string WeatherEndpoint { get; set; } = "/v7/weather/24h";

    public string GeoLookupEndpoint { get; set; } = "/geo/v2/city/lookup";

    public int GeoSearchNumber { get; set; } = 20;

    public int RequestTimeoutSeconds { get; set; } = 10;
}
