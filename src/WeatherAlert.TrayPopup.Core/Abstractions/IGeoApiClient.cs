using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IGeoApiClient
{
    Task<IReadOnlyList<GeoCity>> SearchCitiesInChinaAsync(string keyword, CancellationToken cancellationToken);

    Task<GeoCity?> LookupByCoordinatesAsync(double longitude, double latitude, CancellationToken cancellationToken);
}
