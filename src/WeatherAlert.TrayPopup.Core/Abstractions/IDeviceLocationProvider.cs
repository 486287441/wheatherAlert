namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IDeviceLocationProvider
{
    Task<DeviceGeoPosition?> TryGetCurrentPositionAsync(CancellationToken cancellationToken);
}

public sealed record DeviceGeoPosition(double Latitude, double Longitude);
