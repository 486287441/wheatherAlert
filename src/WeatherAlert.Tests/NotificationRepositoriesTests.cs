using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherAlert.Core.Models;
using WeatherAlert.Infrastructure.Persistence;
using Xunit;

namespace WeatherAlert.Tests;

public sealed class NotificationRepositoriesTests
{
    [Fact]
    public async Task NotificationStateRepository_SameCityAndDate_IsDeduplicated()
    {
        var dbPath = CreateTempDbPath();
        var factory = await CreateInitializedFactoryAsync(dbPath);
        var repository = new NotificationStateRepository(factory);

        var city = "101010100";
        var date = new DateOnly(2026, 5, 28);
        await repository.MarkNotifiedAsync(city, date, "hash-1", CancellationToken.None);
        await repository.MarkNotifiedAsync(city, date, "hash-2", CancellationToken.None);

        var exists = await repository.HasNotifiedAsync(city, date, CancellationToken.None);

        Assert.True(exists);

        // Simulate restart by recreating factory/repository with same sqlite path.
        var restartedFactory = await CreateInitializedFactoryAsync(dbPath);
        var restartedRepository = new NotificationStateRepository(restartedFactory);
        var existsAfterRestart = await restartedRepository.HasNotifiedAsync(city, date, CancellationToken.None);
        Assert.True(existsAfterRestart);
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
