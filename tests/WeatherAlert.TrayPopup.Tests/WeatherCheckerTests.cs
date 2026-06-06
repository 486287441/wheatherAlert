using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.App.Configuration;
using WeatherAlert.TrayPopup.App.Notifications;
using WeatherAlert.TrayPopup.App.Services;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Core.Services;
using WeatherAlert.TrayPopup.Infrastructure.Weather;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class WeatherCheckerTests
{
    [Fact]
    public async Task CheckAsync_ApiReturnsRain_ProducesTodayTomorrowSummaries()
    {
        var now = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.FromHours(8));
        var checker = new WeatherChecker(
            new FixedClock(now),
            new FakeWeatherApiClient(new[]
            {
                new HourlyForecast(new DateTimeOffset(2026, 5, 28, 11, 0, 0, TimeSpan.FromHours(8)), 1.2, 70, "Rain"),
                new HourlyForecast(new DateTimeOffset(2026, 5, 29, 8, 0, 0, TimeSpan.FromHours(8)), 0.4, 30, "Light rain")
            }),
            new RainDetectionService(),
            new InMemoryNotificationStateRepository(),
            new InMemoryNotificationHistoryRepository(),
            new InMemoryAppStateRepository(),
            new NullToastNotificationService(),
            Options.Create(new WeatherOptions { DefaultCityCode = "101010100" }),
            NullLogger<WeatherChecker>.Instance);

        var result = await checker.CheckAsync(CancellationToken.None);

        Assert.True(result.Today.HasRain);
        Assert.True(result.Tomorrow.HasRain);
    }

    [Fact]
    public async Task CheckAsync_NewRainNotification_ShowsToast()
    {
        var toast = new RecordingToastNotificationService();
        var checker = CreateChecker(
            new DateTimeOffset(2026, 6, 6, 10, 0, 0, TimeSpan.FromHours(8)),
            new[]
            {
                new HourlyForecast(new DateTimeOffset(2026, 6, 7, 8, 0, 0, TimeSpan.FromHours(8)), 1.2, 70, "Rain")
            },
            new InMemoryNotificationStateRepository(),
            new InMemoryNotificationHistoryRepository(),
            toast);

        await checker.CheckAsync(CancellationToken.None);

        Assert.Single(toast.Warnings);
        Assert.Equal("降雨提醒 · 明天", toast.Warnings[0].Title);
    }

    [Fact]
    public async Task CheckAsync_SuppressToast_DoesNotShowToast()
    {
        var toast = new RecordingToastNotificationService();
        var checker = CreateChecker(
            new DateTimeOffset(2026, 6, 6, 10, 0, 0, TimeSpan.FromHours(8)),
            new[]
            {
                new HourlyForecast(new DateTimeOffset(2026, 6, 7, 8, 0, 0, TimeSpan.FromHours(8)), 1.2, 70, "Rain")
            },
            new InMemoryNotificationStateRepository(),
            new InMemoryNotificationHistoryRepository(),
            toast);

        await checker.CheckAsync(CancellationToken.None, showToastNotifications: false);

        Assert.Empty(toast.Warnings);
    }

    [Fact]
    public async Task CheckAsync_TomorrowRainThenSameDayToday_RaisesBothNotifications()
    {
        var history = new InMemoryNotificationHistoryRepository();
        var state = new InMemoryNotificationStateRepository();
        var hourly = new[]
        {
            new HourlyForecast(new DateTimeOffset(2026, 6, 7, 8, 0, 0, TimeSpan.FromHours(8)), 1.2, 70, "Rain")
        };

        var dayBefore = new DateTimeOffset(2026, 6, 6, 10, 0, 0, TimeSpan.FromHours(8));
        var checkerDayBefore = CreateChecker(dayBefore, hourly, state, history);
        await checkerDayBefore.CheckAsync(CancellationToken.None);

        var rainDay = new DateTimeOffset(2026, 6, 7, 8, 0, 0, TimeSpan.FromHours(8));
        var checkerRainDay = CreateChecker(rainDay, hourly, state, history);
        await checkerRainDay.CheckAsync(CancellationToken.None);

        Assert.Equal(2, history.Count);
        Assert.Equal("降雨提醒 · 明天", history.Entries[0].Title);
        Assert.Equal("降雨提醒 · 今天", history.Entries[1].Title);
    }

    [Fact]
    public async Task CheckAsync_ConsecutiveApiFailures_WriteErrorHistoryOnlyOnce()
    {
        var now = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.FromHours(8));
        var history = new CountingNotificationHistoryRepository();
        var state = new InMemoryAppStateRepository();
        var checker = new WeatherChecker(
            new FixedClock(now),
            new ThrowingWeatherApiClient(),
            new RainDetectionService(),
            new InMemoryNotificationStateRepository(),
            history,
            state,
            new NullToastNotificationService(),
            Options.Create(new WeatherOptions { DefaultCityCode = "101010100" }),
            NullLogger<WeatherChecker>.Instance);

        await checker.CheckAsync(CancellationToken.None);
        await checker.CheckAsync(CancellationToken.None);

        Assert.Equal(1, history.Count);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset now) => Now = now;

        public DateTimeOffset Now { get; }
    }

    private sealed class FakeWeatherApiClient : IWeatherApiClient
    {
        private readonly IReadOnlyList<HourlyForecast> _data;

        public FakeWeatherApiClient(IReadOnlyList<HourlyForecast> data)
        {
            _data = data;
        }

        public Task<IReadOnlyList<HourlyForecast>> GetHourlyForecastAsync(string cityCode, CancellationToken cancellationToken)
            => Task.FromResult(_data);
    }

    private static WeatherChecker CreateChecker(
        DateTimeOffset now,
        IReadOnlyList<HourlyForecast> hourly,
        InMemoryNotificationStateRepository state,
        InMemoryNotificationHistoryRepository history,
        IToastNotificationService? toastNotificationService = null)
        => new(
            new FixedClock(now),
            new FakeWeatherApiClient(hourly),
            new RainDetectionService(),
            state,
            history,
            new InMemoryAppStateRepository(),
            toastNotificationService ?? new NullToastNotificationService(),
            Options.Create(new WeatherOptions { DefaultCityCode = "101010100" }),
            NullLogger<WeatherChecker>.Instance);

    private sealed class RecordingToastNotificationService : IToastNotificationService
    {
        public List<(string Title, string Body)> Infos { get; } = new();
        public List<(string Title, string Body)> Warnings { get; } = new();
        public List<(string Title, string Body)> Errors { get; } = new();

        public void ShowInfo(string title, string body) => Infos.Add((title, body));

        public void ShowWarning(string title, string body) => Warnings.Add((title, body));

        public void ShowError(string title, string body) => Errors.Add((title, body));
    }

    private sealed class InMemoryNotificationStateRepository : INotificationStateRepository
    {
        private readonly HashSet<string> _states = new();

        public Task<bool> HasNotifiedAsync(
            string cityCode,
            DateOnly targetDate,
            RainDayPerspective perspective,
            CancellationToken cancellationToken)
        {
            var stateKey = RainNotificationStateKey.Format(targetDate, perspective);
            if (_states.Contains($"{cityCode}|{stateKey}"))
            {
                return Task.FromResult(true);
            }

            if (perspective == RainDayPerspective.Tomorrow
                && _states.Contains($"{cityCode}|{RainNotificationStateKey.FormatLegacy(targetDate)}"))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task MarkNotifiedAsync(
            string cityCode,
            DateOnly targetDate,
            RainDayPerspective perspective,
            string messageHash,
            CancellationToken cancellationToken)
        {
            _states.Add($"{cityCode}|{RainNotificationStateKey.Format(targetDate, perspective)}");
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationHistoryRepository : INotificationHistoryRepository
    {
        public List<NotificationHistoryEntry> Entries { get; } = new();

        public int Count => Entries.Count;

        public Task AddAsync(NotificationHistoryEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificationHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<NotificationHistoryEntry>>(Entries);
    }

    private sealed class InMemoryAppStateRepository : IAppStateRepository
    {
        private readonly Dictionary<string, string> _store = new();

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);

        public Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingWeatherApiClient : IWeatherApiClient
    {
        public Task<IReadOnlyList<HourlyForecast>> GetHourlyForecastAsync(string cityCode, CancellationToken cancellationToken)
            => throw new WeatherApiException("network", WeatherApiErrorKind.Network);
    }

    private sealed class CountingNotificationHistoryRepository : INotificationHistoryRepository
    {
        public int Count { get; private set; }

        public Task AddAsync(NotificationHistoryEntry entry, CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificationHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<NotificationHistoryEntry>>(Array.Empty<NotificationHistoryEntry>());
    }
}
