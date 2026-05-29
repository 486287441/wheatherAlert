using Microsoft.Extensions.Logging.Abstractions;
using WeatherAlert.TrayPopup.App.Services;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class CityLocationServiceTests
{
    [Fact]
    public async Task EnsureLocatedCityOnStartupAsync_SetsCurrentWhenMissing()
    {
        var state = new InMemoryAppStateRepository();
        var catalog = new FakeCityCatalog();
        var service = new CityLocationService(
            new FakeGeoApiClient(),
            new FakeDeviceLocationProvider(),
            state,
            catalog,
            NullLogger<CityLocationService>.Instance);

        await service.EnsureLocatedCityOnStartupAsync(CancellationToken.None);

        var current = await service.GetCurrentCityAsync(CancellationToken.None);
        var located = await service.GetLocatedCityAsync(CancellationToken.None);

        Assert.NotNull(current);
        Assert.Equal("101010100", current!.Value.Code);
        Assert.NotNull(located);
        Assert.Equal("101010100", located!.Value.Code);
    }

    private sealed class FakeGeoApiClient : IGeoApiClient
    {
        public Task<IReadOnlyList<GeoCity>> SearchCitiesInChinaAsync(string keyword, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GeoCity>>(Array.Empty<GeoCity>());

        public Task<GeoCity?> LookupByCoordinatesAsync(double longitude, double latitude, CancellationToken cancellationToken)
            => Task.FromResult<GeoCity?>(new GeoCity("101010100", "北京", "北京市", "北京", "中国"));
    }

    private sealed class FakeDeviceLocationProvider : IDeviceLocationProvider
    {
        public Task<DeviceGeoPosition?> TryGetCurrentPositionAsync(CancellationToken cancellationToken)
            => Task.FromResult<DeviceGeoPosition?>(new DeviceGeoPosition(39.92, 116.41));
    }

    private sealed class FakeCityCatalog : ICityCatalog
    {
        public IReadOnlyList<CityCatalogGroup> GetGroups() => Array.Empty<CityCatalogGroup>();

        public IReadOnlyList<ChinaCityEntry> GetAllCities() => Array.Empty<ChinaCityEntry>();

        public ChinaCityEntry? FindById(string cityId)
            => cityId == "101010100"
                ? new ChinaCityEntry("101010100", "北京", "北京市", "北京")
                : null;
    }

    private sealed class InMemoryAppStateRepository : IAppStateRepository
    {
        private readonly Dictionary<string, string> _store = new();

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);

        public Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }
    }
}
