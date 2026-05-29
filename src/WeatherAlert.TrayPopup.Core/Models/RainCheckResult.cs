namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record RainCheckResult(
    DailyRainSummary Today,
    DailyRainSummary Tomorrow)
{
    public bool HasAnyRain => Today.HasRain || Tomorrow.HasRain;
}
