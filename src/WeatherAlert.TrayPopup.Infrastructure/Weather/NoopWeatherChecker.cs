using Microsoft.Extensions.Logging;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Infrastructure.Weather;

public sealed class NoopWeatherChecker : IWeatherChecker
{
    private readonly IClock _clock;
    private readonly ILogger<NoopWeatherChecker> _logger;

    public NoopWeatherChecker(IClock clock, ILogger<NoopWeatherChecker> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public Task<RainCheckResult> CheckAsync(CancellationToken cancellationToken, bool showToastNotifications = true)
    {
        _logger.LogInformation("No-op weather checker executed at {Now}.", _clock.Now);
        return Task.FromResult(new RainCheckResult(
            new DailyRainSummary(DateOnly.FromDateTime(_clock.Now.Date), false, Array.Empty<RainTimeRange>(), "none"),
            new DailyRainSummary(DateOnly.FromDateTime(_clock.Now.Date.AddDays(1)), false, Array.Empty<RainTimeRange>(), "none")));
    }
}
