using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace WeatherAlert.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    private readonly string _databasePath;

    public SqliteConnectionFactory(IOptions<SqliteOptions> options)
    {
        _databasePath = options.Value.DatabasePath;
    }

    public SqliteConnection CreateConnection()
    {
        var fullPath = Path.GetFullPath(_databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new SqliteConnection($"Data Source={fullPath}");
    }
}
