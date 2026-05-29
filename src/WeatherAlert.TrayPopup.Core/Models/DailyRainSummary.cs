namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record DailyRainSummary(
    DateOnly Date,
    bool HasRain,
    IReadOnlyList<RainTimeRange> TimeRanges,
    string IntensityLabel);
