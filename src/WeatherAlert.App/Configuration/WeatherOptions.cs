namespace WeatherAlert.App.Configuration;

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiBaseUrl { get; set; } = "https://devapi.qweather.com";

    public string WeatherEndpoint { get; set; } = "/v7/weather/24h";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public string DatabasePath { get; set; } = "data/weather-alert.db";

    public string DefaultCityCode { get; set; } = "101010100";

    public int PollingMinutes { get; set; } = 60;
}
