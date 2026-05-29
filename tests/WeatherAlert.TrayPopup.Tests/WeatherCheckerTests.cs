using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.App.Configuration;
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
            Options.Create(new WeatherOptions { DefaultCityCode = "101010100" }),
            NullLogger<WeatherChecker>.Instance);

        var result = await checker.CheckAsync(CancellationToken.None);

        Assert.True(result.Today.HasRain);
        Assert.True(result.Tomorrow.HasRain);
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

    private sealed class InMemoryNotificationStateRepository : INotificationStateRepository
    {
        private readonly HashSet<string> _states = new();

        public Task<bool> HasNotifiedAsync(string cityCode, DateOnly targetDate, CancellationToken cancellationToken)
            => Task.FromResult(_states.Contains($"{cityCode}|{targetDate:yyyy-MM-dd}"));

        public Task MarkNotifiedAsync(string cityCode, DateOnly targetDate, string messageHash, CancellationToken cancellationToken)
        {
            _states.Add($"{cityCode}|{targetDate:yyyy-MM-dd}");
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationHistoryRepository : INotificationHistoryRepository
    {
        public Task AddAsync(NotificationHistoryEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<NotificationHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<NotificationHistoryEntry>>(Array.Empty<NotificationHistoryEntry>());
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
