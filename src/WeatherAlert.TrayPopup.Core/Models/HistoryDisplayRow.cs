namespace WeatherAlert.TrayPopup.Core.Models;

public sealed class HistoryDisplayRow
{
    public required string NotifiedAt { get; init; }

    public required string Type { get; init; }

    public required string City { get; init; }

    public required string Detail { get; init; }
}
