using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.App.Configuration;
using WeatherAlert.TrayPopup.Core.Abstractions;

namespace WeatherAlert.TrayPopup.App.Services;

public sealed class WeatherWorker : BackgroundService
{
    private readonly ILogger<WeatherWorker> _logger;
    private readonly IWeatherChecker _weatherChecker;
    private readonly IOptions<WeatherOptions> _options;

    public WeatherWorker(
        ILogger<WeatherWorker> logger,
        IWeatherChecker weatherChecker,
        IOptions<WeatherOptions> options)
    {
        _logger = logger;
        _weatherChecker = weatherChecker;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingMinutes = Math.Max(1, _options.Value.PollingMinutes);
        _logger.LogInformation("Weather worker started with polling interval {PollingMinutes} minutes.", pollingMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _weatherChecker.CheckAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(pollingMinutes), stoppingToken);
        }
    }
}
