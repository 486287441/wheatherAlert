using Microsoft.Data.Sqlite;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Infrastructure.Persistence;

public sealed class HourlyForecastCacheRepository : IHourlyForecastCacheRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public HourlyForecastCacheRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertAsync(
        string cityCode,
        IReadOnlyList<HourlyForecast> forecasts,
        DateTimeOffset capturedAt,
        CancellationToken cancellationToken)
    {
        if (forecasts.Count == 0)
        {
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await PurgeExpiredAsync(connection, transaction, capturedAt, cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO hourly_forecast_cache (
                city_code,
                forecast_time,
                precipitation_mm,
                precipitation_probability,
                condition_text,
                captured_at
            )
            VALUES (
                $city_code,
                $forecast_time,
                $precipitation_mm,
                $precipitation_probability,
                $condition_text,
                $captured_at
            )
            ON CONFLICT(city_code, forecast_time) DO UPDATE SET
                precipitation_mm = excluded.precipitation_mm,
                precipitation_probability = excluded.precipitation_probability,
                condition_text = excluded.condition_text,
                captured_at = excluded.captured_at;
            """;

        var cityParam = command.CreateParameter();
        cityParam.ParameterName = "$city_code";
        command.Parameters.Add(cityParam);

        var forecastTimeParam = command.CreateParameter();
        forecastTimeParam.ParameterName = "$forecast_time";
        command.Parameters.Add(forecastTimeParam);

        var precipMmParam = command.CreateParameter();
        precipMmParam.ParameterName = "$precipitation_mm";
        command.Parameters.Add(precipMmParam);

        var precipPopParam = command.CreateParameter();
        precipPopParam.ParameterName = "$precipitation_probability";
        command.Parameters.Add(precipPopParam);

        var conditionParam = command.CreateParameter();
        conditionParam.ParameterName = "$condition_text";
        command.Parameters.Add(conditionParam);

        var capturedAtParam = command.CreateParameter();
        capturedAtParam.ParameterName = "$captured_at";
        command.Parameters.Add(capturedAtParam);

        cityParam.Value = cityCode;
        capturedAtParam.Value = capturedAt.ToString("O");

        foreach (var forecast in forecasts)
        {
            forecastTimeParam.Value = forecast.ForecastTime.ToString("O");
            precipMmParam.Value = forecast.PrecipitationMm;
            precipPopParam.Value = forecast.PrecipitationProbability;
            conditionParam.Value = (object?)forecast.ConditionText ?? DBNull.Value;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HourlyForecast>> GetRangeAsync(
        string cityCode,
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT forecast_time, precipitation_mm, precipitation_probability, condition_text
            FROM hourly_forecast_cache
            WHERE city_code = $city_code
              AND forecast_time >= $start_inclusive
              AND forecast_time < $end_exclusive
            ORDER BY forecast_time;
            """;
        command.Parameters.AddWithValue("$city_code", cityCode);
        command.Parameters.AddWithValue("$start_inclusive", startInclusive.ToString("O"));
        command.Parameters.AddWithValue("$end_exclusive", endExclusive.ToString("O"));

        var result = new List<HourlyForecast>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new HourlyForecast(
                DateTimeOffset.Parse(reader.GetString(0)),
                reader.GetDouble(1),
                reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return result;
    }

    private static async Task PurgeExpiredAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DateTimeOffset capturedAt,
        CancellationToken cancellationToken)
    {
        var localCapturedAt = capturedAt.ToLocalTime();
        var cutoff = new DateTimeOffset(localCapturedAt.Date, localCapturedAt.Offset);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM hourly_forecast_cache
            WHERE forecast_time < $cutoff;
            """;
        command.Parameters.AddWithValue("$cutoff", cutoff.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
