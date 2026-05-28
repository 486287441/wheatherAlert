namespace WeatherAlert.Infrastructure.Weather;

public sealed class HeWeatherClientOptions
{
    public const string SectionName = "Weather";

    public string ApiKey { get; set; } = string.Empty;

    public string WeatherEndpoint { get; set; } = "/v7/weather/24h";

    public int RequestTimeoutSeconds { get; set; } = 10;
}
