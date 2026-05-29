using WeatherAlert.TrayPopup.Core;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.App.Services;

public sealed class CityLocationService : ICityLocationService
{
    private readonly IGeoApiClient _geoApiClient;
    private readonly IDeviceLocationProvider _deviceLocationProvider;
    private readonly IAppStateRepository _appStateRepository;
    private readonly ICityCatalog _cityCatalog;
    private readonly ILogger<CityLocationService> _logger;

    public CityLocationService(
        IGeoApiClient geoApiClient,
        IDeviceLocationProvider deviceLocationProvider,
        IAppStateRepository appStateRepository,
        ICityCatalog cityCatalog,
        ILogger<CityLocationService> logger)
    {
        _geoApiClient = geoApiClient;
        _deviceLocationProvider = deviceLocationProvider;
        _appStateRepository = appStateRepository;
        _cityCatalog = cityCatalog;
        _logger = logger;
    }

    public async Task<GeoCity?> TryDetectLocatedCityAsync(CancellationToken cancellationToken)
    {
        var position = await _deviceLocationProvider.TryGetCurrentPositionAsync(cancellationToken).ConfigureAwait(false);
        if (position is null)
        {
            _logger.LogInformation("Device location unavailable or permission denied.");
            return null;
        }

        var city = await _geoApiClient
            .LookupByCoordinatesAsync(position.Longitude, position.Latitude, cancellationToken)
            .ConfigureAwait(false);
        if (city is null)
        {
            _logger.LogWarning(
                "Geo lookup returned no city for coordinates {Longitude},{Latitude}.",
                position.Longitude,
                position.Latitude);
        }

        return city;
    }

    public async Task<(string Code, string Name)?> GetCurrentCityAsync(CancellationToken cancellationToken)
    {
        var code = await _appStateRepository.GetValueAsync(AppStateKeys.CurrentCityCode, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var name = await _appStateRepository.GetValueAsync(AppStateKeys.CurrentCityName, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return (code, name);
        }

        var catalogEntry = _cityCatalog.FindById(code);
        return catalogEntry is null ? (code, code) : (code, catalogEntry.DisplayName);
    }

    public async Task<(string Code, string Name)?> GetLocatedCityAsync(CancellationToken cancellationToken)
    {
        var code = await _appStateRepository.GetValueAsync(AppStateKeys.LocatedCityCode, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var name = await _appStateRepository.GetValueAsync(AppStateKeys.LocatedCityName, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(name) ? (code, code) : (code, name);
    }

    public Task SetCurrentCityAsync(GeoCity city, CancellationToken cancellationToken)
        => SetCurrentCityAsync(city.Id, city.DisplayName, cancellationToken);

    public Task SetCurrentCityAsync(ChinaCityEntry city, CancellationToken cancellationToken)
        => SetCurrentCityAsync(city.Id, city.DisplayName, cancellationToken);

    public async Task PersistLocatedCityAsync(GeoCity city, CancellationToken cancellationToken)
    {
        await _appStateRepository.SetValueAsync(AppStateKeys.LocatedCityCode, city.Id, cancellationToken).ConfigureAwait(false);
        await _appStateRepository.SetValueAsync(AppStateKeys.LocatedCityName, city.DisplayName, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureLocatedCityOnStartupAsync(CancellationToken cancellationToken)
    {
        var current = await GetCurrentCityAsync(cancellationToken).ConfigureAwait(false);
        var located = await TryDetectLocatedCityAsync(cancellationToken).ConfigureAwait(false);
        if (located is null)
        {
            return;
        }

        await PersistLocatedCityAsync(located, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            await SetCurrentCityAsync(located, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Initial city set from device location: {CityName} ({CityCode}).", located.DisplayName, located.Id);
        }
    }

    private async Task SetCurrentCityAsync(string code, string name, CancellationToken cancellationToken)
    {
        await _appStateRepository.SetValueAsync(AppStateKeys.CurrentCityCode, code, cancellationToken).ConfigureAwait(false);
        await _appStateRepository.SetValueAsync(AppStateKeys.CurrentCityName, name, cancellationToken).ConfigureAwait(false);
    }
}
