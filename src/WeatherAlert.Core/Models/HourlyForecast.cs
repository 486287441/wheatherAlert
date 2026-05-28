namespace WeatherAlert.Core.Models;

public sealed record HourlyForecast(
    DateTimeOffset ForecastTime,
    double PrecipitationMm,
    int PrecipitationProbability,
    string? ConditionText);
