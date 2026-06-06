namespace WeatherAlert.TrayPopup.Wpf.ViewModels;

public sealed class HourlyForecastItemViewModel
{
    public bool HasData { get; init; }

    public string? IconPath { get; init; }

    public required string TimeLabel { get; init; }

    public required string ConditionText { get; init; }

    public string? DetailText { get; init; }
}
