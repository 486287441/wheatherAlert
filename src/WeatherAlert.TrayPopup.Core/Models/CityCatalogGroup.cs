namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record CityCatalogGroup(string Province, IReadOnlyList<ChinaCityEntry> Cities);
