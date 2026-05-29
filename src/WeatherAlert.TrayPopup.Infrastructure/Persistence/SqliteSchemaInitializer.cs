using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WeatherAlert.TrayPopup.Infrastructure.Persistence;

public sealed class SqliteSchemaInitializer : IHostedService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<SqliteSchemaInitializer> _logger;

    public SqliteSchemaInitializer(
        SqliteConnectionFactory connectionFactory,
        ILogger<SqliteSchemaInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS app_state (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS rain_notification_state (
                city_code TEXT NOT NULL,
                target_date TEXT NOT NULL,
                notified_at TEXT NOT NULL,
                message_hash TEXT NOT NULL,
                PRIMARY KEY (city_code, target_date)
            );

            CREATE TABLE IF NOT EXISTS notification_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                created_at TEXT NOT NULL,
                type TEXT NOT NULL,
                city_code TEXT NOT NULL,
                title TEXT NOT NULL,
                body TEXT NOT NULL,
                meta_json TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("SQLite schema ensured.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
