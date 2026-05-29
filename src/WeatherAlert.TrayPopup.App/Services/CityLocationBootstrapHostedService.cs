using WeatherAlert.TrayPopup.Core.Abstractions;

namespace WeatherAlert.TrayPopup.App.Services;

public sealed class CityLocationBootstrapHostedService : BackgroundService
{
    private readonly ICityLocationService _cityLocationService;
    private readonly ILogger<CityLocationBootstrapHostedService> _logger;

    public CityLocationBootstrapHostedService(
        ICityLocationService cityLocationService,
        ILogger<CityLocationBootstrapHostedService> logger)
    {
        _cityLocationService = cityLocationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _cityLocationService.EnsureLocatedCityOnStartupAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Startup city location bootstrap failed.");
        }
    }
}
