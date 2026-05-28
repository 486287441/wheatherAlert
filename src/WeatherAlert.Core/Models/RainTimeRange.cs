namespace WeatherAlert.Core.Models;

public sealed record RainTimeRange(
    DateTimeOffset Start,
    DateTimeOffset End);
