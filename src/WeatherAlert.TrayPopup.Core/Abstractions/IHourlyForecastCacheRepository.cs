using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IHourlyForecastCacheRepository
{
    Task UpsertAsync(
        string cityCode,
        IReadOnlyList<HourlyForecast> forecasts,
        DateTimeOffset capturedAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HourlyForecast>> GetRangeAsync(
        string cityCode,
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        CancellationToken cancellationToken);
}
