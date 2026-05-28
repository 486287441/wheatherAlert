namespace WeatherAlert.Infrastructure.Weather;

public sealed class WeatherApiException : Exception
{
    public WeatherApiException(string message, WeatherApiErrorKind errorKind)
        : base(message)
    {
        ErrorKind = errorKind;
    }

    public WeatherApiErrorKind ErrorKind { get; }
}
