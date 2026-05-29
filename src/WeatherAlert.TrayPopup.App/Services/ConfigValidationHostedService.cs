using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.App.Configuration;

namespace WeatherAlert.TrayPopup.App.Services;

public sealed class ConfigValidationHostedService : IHostedService
{
    private readonly ILogger<ConfigValidationHostedService> _logger;
    private readonly IOptions<WeatherOptions> _weatherOptions;

    public ConfigValidationHostedService(
        ILogger<ConfigValidationHostedService> logger,
        IOptions<WeatherOptions> weatherOptions)
    {
        _logger = logger;
        _weatherOptions = weatherOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _weatherOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogError(
                "Missing required configuration: {Section}:{Key}.",
                WeatherOptions.SectionName,
                nameof(WeatherOptions.ApiKey));
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _))
        {
            _logger.LogError(
                "Invalid weather API base URL: {ApiBaseUrl}.",
                options.ApiBaseUrl);
        }

        if (options.PollingMinutes <= 0)
        {
            _logger.LogError(
                "Invalid polling interval: {PollingMinutes}. It must be greater than zero.",
                options.PollingMinutes);
        }

        _logger.LogInformation(
            "Configuration loaded. Default city: {CityCode}, Polling minutes: {PollingMinutes}, endpoint: {WeatherEndpoint}.",
            options.DefaultCityCode,
            options.PollingMinutes,
            options.WeatherEndpoint);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
