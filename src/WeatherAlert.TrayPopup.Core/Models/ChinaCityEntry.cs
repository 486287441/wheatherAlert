namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record ChinaCityEntry(
    string Id,
    string Name,
    string Province,
    string? Admin2)
{
    public string GroupKey => Province;

    public string DisplayName => string.IsNullOrWhiteSpace(Admin2) || Admin2 == Name || Admin2 == Province
        ? Name
        : $"{Name} · {Admin2}";
}
