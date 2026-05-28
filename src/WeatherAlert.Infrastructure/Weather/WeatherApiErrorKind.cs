namespace WeatherAlert.Infrastructure.Weather;

public enum WeatherApiErrorKind
{
    Unknown = 0,
    Timeout = 1,
    Network = 2,
    Authentication = 3,
    BadResponse = 4,
    ServerError = 5
}
