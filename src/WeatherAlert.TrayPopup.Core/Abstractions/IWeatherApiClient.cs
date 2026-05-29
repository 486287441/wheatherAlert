using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IWeatherApiClient
{
    Task<IReadOnlyList<HourlyForecast>> GetHourlyForecastAsync(
        string cityCode,
        CancellationToken cancellationToken);
}
