namespace WeatherAlert.Core.Abstractions;

public interface IClock
{
    DateTimeOffset Now { get; }
}
