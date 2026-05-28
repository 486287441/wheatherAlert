using WeatherAlert.Core.Models;

namespace WeatherAlert.Core.Abstractions;

public interface IWeatherApiClient
{
    Task<IReadOnlyList<HourlyForecast>> GetHourlyForecastAsync(
        string cityCode,
        CancellationToken cancellationToken);
}
