using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface ICityCatalog
{
    IReadOnlyList<CityCatalogGroup> GetGroups();

    IReadOnlyList<ChinaCityEntry> GetAllCities();

    ChinaCityEntry? FindById(string cityId);
}
