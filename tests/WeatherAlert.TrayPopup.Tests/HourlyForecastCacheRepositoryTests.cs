using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Infrastructure.Persistence;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class HourlyForecastCacheRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_PersistsAndReturnsRange()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new HourlyForecastCacheRepository(factory);
        var offset = TimeSpan.FromHours(8);
        var capturedAt = new DateTimeOffset(2026, 6, 6, 12, 0, 0, offset);
        var forecasts = new[]
        {
            new HourlyForecast(new DateTimeOffset(2026, 6, 6, 8, 0, 0, offset), 0.2, 40, "\u591a\u4e91"),
            new HourlyForecast(new DateTimeOffset(2026, 6, 6, 9, 0, 0, offset), 0.5, 60, "\u5c0f\u96e8")
        };

        await repository.UpsertAsync("101010100", forecasts, capturedAt, CancellationToken.None);

        var windowStart = new DateTimeOffset(2026, 6, 6, 0, 0, 0, offset);
        var windowEnd = windowStart.AddDays(2);
        var loaded = await repository.GetRangeAsync("101010100", windowStart, windowEnd, CancellationToken.None);

        Assert.Equal(2, loaded.Count);
        Assert.Equal("\u5c0f\u96e8", loaded[1].ConditionText);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingHour()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new HourlyForecastCacheRepository(factory);
        var offset = TimeSpan.FromHours(8);
        var hour = new DateTimeOffset(2026, 6, 6, 10, 0, 0, offset);
        var capturedAt = new DateTimeOffset(2026, 6, 6, 12, 0, 0, offset);

        await repository.UpsertAsync(
            "101010100",
            new[] { new HourlyForecast(hour, 0.1, 20, "\u591a\u4e91") },
            capturedAt,
            CancellationToken.None);
        await repository.UpsertAsync(
            "101010100",
            new[] { new HourlyForecast(hour, 1.2, 80, "\u5927\u96e8") },
            capturedAt.AddHours(1),
            CancellationToken.None);

        var loaded = await repository.GetRangeAsync(
            "101010100",
            hour,
            hour.AddHours(1),
            CancellationToken.None);

        Assert.Single(loaded);
        Assert.Equal("\u5927\u96e8", loaded[0].ConditionText);
        Assert.Equal(80, loaded[0].PrecipitationProbability);
    }

    private static async Task<SqliteConnectionFactory> CreateInitializedFactoryAsync(string dbPath)
    {
        var options = Options.Create(new SqliteOptions { DatabasePath = dbPath });
        var factory = new SqliteConnectionFactory(options);
        var initializer = new SqliteSchemaInitializer(factory, NullLogger<SqliteSchemaInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);
        return factory;
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), $"weather-alert-cache-tests-{Guid.NewGuid():N}.db");
}
