namespace WeatherAlert.TrayPopup.Infrastructure.Persistence;

public sealed class SqliteOptions
{
    public const string SectionName = "Weather";

    public string DatabasePath { get; set; } = "data/weather-alert.db";
}
