using WeatherAlert.TrayPopup.Core.Abstractions;
using Windows.Devices.Geolocation;

namespace WeatherAlert.TrayPopup.App.Location;

public sealed class WindowsGeolocationProvider : IDeviceLocationProvider
{
    public async Task<DeviceGeoPosition?> TryGetCurrentPositionAsync(CancellationToken cancellationToken)
    {
        var access = await Geolocator.RequestAccessAsync().AsTask(cancellationToken).ConfigureAwait(false);
        if (access != GeolocationAccessStatus.Allowed)
        {
            return null;
        }

        var geolocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.Default,
            DesiredAccuracyInMeters = 500
        };

        var position = await geolocator
            .GetGeopositionAsync(maximumAge: TimeSpan.FromMinutes(5), timeout: TimeSpan.FromSeconds(15))
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

        var point = position.Coordinate.Point.Position;
        return new DeviceGeoPosition(point.Latitude, point.Longitude);
    }
}
