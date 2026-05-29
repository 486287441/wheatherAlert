using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WeatherAlert.TrayPopup.App.Configuration;
using WeatherAlert.TrayPopup.Core;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Infrastructure.Weather;

namespace WeatherAlert.TrayPopup.App.Services;

public sealed class WeatherChecker : IWeatherChecker
{
    private readonly IClock _clock;
    private readonly IWeatherApiClient _weatherApiClient;
    private readonly IRainDetectionService _rainDetectionService;
    private readonly INotificationStateRepository _notificationStateRepository;
    private readonly INotificationHistoryRepository _notificationHistoryRepository;
    private readonly IAppStateRepository _appStateRepository;
    private readonly IOptions<WeatherOptions> _options;
    private readonly ILogger<WeatherChecker> _logger;

    public WeatherChecker(
        IClock clock,
        IWeatherApiClient weatherApiClient,
        IRainDetectionService rainDetectionService,
        INotificationStateRepository notificationStateRepository,
        INotificationHistoryRepository notificationHistoryRepository,
        IAppStateRepository appStateRepository,
        IOptions<WeatherOptions> options,
        ILogger<WeatherChecker> logger)
    {
        _clock = clock;
        _weatherApiClient = weatherApiClient;
        _rainDetectionService = rainDetectionService;
        _notificationStateRepository = notificationStateRepository;
        _notificationHistoryRepository = notificationHistoryRepository;
        _appStateRepository = appStateRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<RainCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cityCode = await ResolveCityCodeAsync(cancellationToken);
            var hourly = await _weatherApiClient.GetHourlyForecastAsync(cityCode, cancellationToken);
            var result = _rainDetectionService.Detect(hourly, _clock.Now);
            await PersistRainNotificationStateAsync(cityCode, result.Today, cancellationToken);
            await PersistRainNotificationStateAsync(cityCode, result.Tomorrow, cancellationToken);
            await _appStateRepository.SetValueAsync("weather_error_notified", "0", cancellationToken);
            _logger.LogInformation(
                "Rain check result generated. Today rain: {TodayHasRain}; Tomorrow rain: {TomorrowHasRain}.",
                result.Today.HasRain,
                result.Tomorrow.HasRain);
            return result;
        }
        catch (WeatherApiException ex)
        {
            _logger.LogError(ex, "Weather API failed with kind {ErrorKind}.", ex.ErrorKind);
            var alreadyNotified = await _appStateRepository.GetValueAsync("weather_error_notified", cancellationToken);
            if (alreadyNotified != "1")
            {
                await _notificationHistoryRepository.AddAsync(
                    new NotificationHistoryEntry(
                        0,
                        _clock.Now,
                        NotificationType.Error,
                        _options.Value.DefaultCityCode,
                        "天气拉取失败",
                        ex.Message,
                        JsonSerializer.Serialize(new { ex.ErrorKind })),
                    cancellationToken);
                await _appStateRepository.SetValueAsync("weather_error_notified", "1", cancellationToken);
            }
            return NoRainFallback();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking weather.");
            return NoRainFallback();
        }
    }

    private async Task<string> ResolveCityCodeAsync(CancellationToken cancellationToken)
    {
        var cityFromState = await _appStateRepository.GetValueAsync(AppStateKeys.CurrentCityCode, cancellationToken);
        return string.IsNullOrWhiteSpace(cityFromState) ? _options.Value.DefaultCityCode : cityFromState;
    }

    private async Task PersistRainNotificationStateAsync(
        string cityCode,
        DailyRainSummary summary,
        CancellationToken cancellationToken)
    {
        if (!summary.HasRain)
        {
            return;
        }

        var alreadyNotified = await _notificationStateRepository.HasNotifiedAsync(cityCode, summary.Date, cancellationToken);
        if (alreadyNotified)
        {
            _logger.LogInformation("Rain notification skipped because state already exists. City: {City}, Date: {Date}.", cityCode, summary.Date);
            return;
        }

        var periodText = RainSummaryFormatter.FormatTimeRanges(summary.TimeRanges);
        var body = $"{periodText} 有降雨（{RainSummaryFormatter.FormatIntensity(summary.IntensityLabel)}）";
        var hash = CreateMessageHash(cityCode, summary.Date, body);

        await _notificationStateRepository.MarkNotifiedAsync(cityCode, summary.Date, hash, cancellationToken);
        await _notificationHistoryRepository.AddAsync(
            new NotificationHistoryEntry(
                0,
                _clock.Now,
                NotificationType.Rain,
                cityCode,
                NotificationHistoryFormatter.FormatRainHistoryTitle(summary.Date, _clock.Now),
                body,
                JsonSerializer.Serialize(new { summary.IntensityLabel, rangeCount = summary.TimeRanges.Count })),
            cancellationToken);
    }

    private static string CreateMessageHash(string cityCode, DateOnly date, string body)
    {
        var plainText = $"{cityCode}|{date:yyyy-MM-dd}|{body}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexString(bytes);
    }

    private RainCheckResult NoRainFallback()
    {
        var today = DateOnly.FromDateTime(_clock.Now.Date);
        return new RainCheckResult(
            new DailyRainSummary(today, false, Array.Empty<RainTimeRange>(), "none"),
            new DailyRainSummary(today.AddDays(1), false, Array.Empty<RainTimeRange>(), "none"));
    }
}
