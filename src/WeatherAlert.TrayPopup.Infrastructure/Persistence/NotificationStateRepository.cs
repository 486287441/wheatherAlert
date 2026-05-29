using Microsoft.Data.Sqlite;
using WeatherAlert.TrayPopup.Core.Abstractions;

namespace WeatherAlert.TrayPopup.Infrastructure.Persistence;

public sealed class NotificationStateRepository : INotificationStateRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public NotificationStateRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasNotifiedAsync(string cityCode, DateOnly targetDate, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM rain_notification_state
            WHERE city_code = $city_code AND target_date = $target_date
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$city_code", cityCode);
        command.Parameters.AddWithValue("$target_date", targetDate.ToString("yyyy-MM-dd"));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task MarkNotifiedAsync(
        string cityCode,
        DateOnly targetDate,
        string messageHash,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO rain_notification_state (city_code, target_date, notified_at, message_hash)
            VALUES ($city_code, $target_date, $notified_at, $message_hash);
            """;
        command.Parameters.AddWithValue("$city_code", cityCode);
        command.Parameters.AddWithValue("$target_date", targetDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$notified_at", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$message_hash", messageHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
