using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Infrastructure.Persistence;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class NotificationRepositoriesTests
{
    [Fact]
    public async Task NotificationStateRepository_SameCityDateAndPerspective_IsDeduplicated()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new NotificationStateRepository(factory);

        var city = "101010100";
        var date = new DateOnly(2026, 5, 28);
        await repository.MarkNotifiedAsync(city, date, RainDayPerspective.Tomorrow, "hash-1", CancellationToken.None);
        await repository.MarkNotifiedAsync(city, date, RainDayPerspective.Tomorrow, "hash-2", CancellationToken.None);

        var exists = await repository.HasNotifiedAsync(city, date, RainDayPerspective.Tomorrow, CancellationToken.None);

        Assert.True(exists);

        // Simulate restart by recreating factory/repository with same sqlite path.
        var restartedFactory = await CreateInitializedFactoryAsync(dbPath);
        var restartedRepository = new NotificationStateRepository(restartedFactory);
        var existsAfterRestart = await restartedRepository.HasNotifiedAsync(
            city,
            date,
            RainDayPerspective.Tomorrow,
            CancellationToken.None);
        Assert.True(existsAfterRestart);
    }

    [Fact]
    public async Task NotificationStateRepository_TomorrowThenToday_AllowsSecondNotification()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new NotificationStateRepository(factory);

        var city = "101010100";
        var date = new DateOnly(2026, 6, 7);
        await repository.MarkNotifiedAsync(city, date, RainDayPerspective.Tomorrow, "hash-tomorrow", CancellationToken.None);

        var tomorrowExists = await repository.HasNotifiedAsync(city, date, RainDayPerspective.Tomorrow, CancellationToken.None);
        var todayExists = await repository.HasNotifiedAsync(city, date, RainDayPerspective.Today, CancellationToken.None);

        Assert.True(tomorrowExists);
        Assert.False(todayExists);
    }

    [Fact]
    public async Task NotificationStateRepository_LegacyTomorrowState_BlocksTomorrowOnly()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new NotificationStateRepository(factory);

        var city = "101010100";
        var date = new DateOnly(2026, 6, 7);
        await repository.MarkNotifiedAsync(city, date, RainDayPerspective.Tomorrow, "legacy-hash", CancellationToken.None);

        await using var connection = factory.CreateConnection();
        await connection.OpenAsync(CancellationToken.None);
        await using var update = connection.CreateCommand();
        update.CommandText = """
            UPDATE rain_notification_state
            SET target_date = $legacy_date
            WHERE city_code = $city_code;
            """;
        update.Parameters.AddWithValue("$legacy_date", RainNotificationStateKey.FormatLegacy(date));
        update.Parameters.AddWithValue("$city_code", city);
        await update.ExecuteNonQueryAsync(CancellationToken.None);

        var tomorrowExists = await repository.HasNotifiedAsync(city, date, RainDayPerspective.Tomorrow, CancellationToken.None);
        var todayExists = await repository.HasNotifiedAsync(city, date, RainDayPerspective.Today, CancellationToken.None);

        Assert.True(tomorrowExists);
        Assert.False(todayExists);
    }

    [Fact]
    public async Task NotificationHistoryRepository_GetRecent_ReturnsLatestNDescending()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new NotificationHistoryRepository(factory);

        await repository.AddAsync(new NotificationHistoryEntry(0, DateTimeOffset.Parse("2026-05-28T08:00:00+08:00"), NotificationType.Rain, "101", "A", "A", "{}"), CancellationToken.None);
        await repository.AddAsync(new NotificationHistoryEntry(0, DateTimeOffset.Parse("2026-05-28T09:00:00+08:00"), NotificationType.Error, "101", "B", "B", "{}"), CancellationToken.None);
        await repository.AddAsync(new NotificationHistoryEntry(0, DateTimeOffset.Parse("2026-05-28T10:00:00+08:00"), NotificationType.Rain, "101", "C", "C", "{}"), CancellationToken.None);

        var recent = await repository.GetRecentAsync(2, CancellationToken.None);

        Assert.Equal(2, recent.Count);
        Assert.Equal("C", recent[0].Title);
        Assert.Equal("B", recent[1].Title);
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
    {
        return Path.Combine(Path.GetTempPath(), $"weather-alert-tests-{Guid.NewGuid():N}.db");
    }
}
