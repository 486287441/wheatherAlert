using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Infrastructure.Persistence;

public sealed class NotificationHistoryRepository : INotificationHistoryRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public NotificationHistoryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(NotificationHistoryEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO notification_history (created_at, type, city_code, title, body, meta_json)
            VALUES ($created_at, $type, $city_code, $title, $body, $meta_json);
            """;
        command.Parameters.AddWithValue("$created_at", entry.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$type", entry.Type.ToString().ToLowerInvariant());
        command.Parameters.AddWithValue("$city_code", entry.CityCode);
        command.Parameters.AddWithValue("$title", entry.Title);
        command.Parameters.AddWithValue("$body", entry.Body);
        command.Parameters.AddWithValue("$meta_json", entry.MetaJson);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var take = Math.Max(1, limit);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, created_at, type, city_code, title, body, meta_json
            FROM notification_history
            ORDER BY created_at DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", take);

        var result = new List<NotificationHistoryEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var typeRaw = reader.GetString(2);
            _ = Enum.TryParse<NotificationType>(typeRaw, true, out var parsedType);
            result.Add(new NotificationHistoryEntry(
                reader.GetInt64(0),
                DateTimeOffset.Parse(reader.GetString(1)),
                parsedType,
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6)));
        }

        return result;
    }
}
