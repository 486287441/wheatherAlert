namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record NotificationHistoryEntry(
    long Id,
    DateTimeOffset CreatedAt,
    NotificationType Type,
    string CityCode,
    string Title,
    string Body,
    string MetaJson);
