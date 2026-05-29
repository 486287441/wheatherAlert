using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IRainDetectionService
{
    RainCheckResult Detect(
        IReadOnlyList<HourlyForecast> hourlyForecasts,
        DateTimeOffset now);
}
