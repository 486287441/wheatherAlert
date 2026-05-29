using WeatherAlert.TrayPopup.Core.Abstractions;

namespace WeatherAlert.TrayPopup.Infrastructure.Persistence;

public sealed class AppStateRepository : IAppStateRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public AppStateRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM app_state WHERE key = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", key);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO app_state (key, value, updated_at)
            VALUES ($key, $value, $updated_at)
            ON CONFLICT(key) DO UPDATE SET
                value = excluded.value,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$updated_at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
