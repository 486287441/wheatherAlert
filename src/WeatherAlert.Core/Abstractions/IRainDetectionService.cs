using WeatherAlert.Core.Models;

namespace WeatherAlert.Core.Abstractions;

public interface IRainDetectionService
{
    RainCheckResult Detect(
        IReadOnlyList<HourlyForecast> hourlyForecasts,
        DateTimeOffset now);
}
