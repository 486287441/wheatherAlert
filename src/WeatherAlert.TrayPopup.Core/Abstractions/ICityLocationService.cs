using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface ICityLocationService
{
    Task<GeoCity?> TryDetectLocatedCityAsync(CancellationToken cancellationToken);

    Task<(string Code, string Name)?> GetCurrentCityAsync(CancellationToken cancellationToken);

    Task<(string Code, string Name)?> GetLocatedCityAsync(CancellationToken cancellationToken);

    Task SetCurrentCityAsync(GeoCity city, CancellationToken cancellationToken);

    Task SetCurrentCityAsync(ChinaCityEntry city, CancellationToken cancellationToken);

    Task PersistLocatedCityAsync(GeoCity city, CancellationToken cancellationToken);

    Task EnsureLocatedCityOnStartupAsync(CancellationToken cancellationToken);
}
