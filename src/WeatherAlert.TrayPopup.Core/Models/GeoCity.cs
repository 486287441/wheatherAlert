namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record GeoCity(
    string Id,
    string Name,
    string? Admin1,
    string? Admin2,
    string Country)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Admin1)
        ? Name
        : Admin1 == Name
            ? $"{Name} · {Admin2}"
            : $"{Name} · {Admin1}";
}
