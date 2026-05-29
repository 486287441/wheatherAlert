namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IClock
{
    DateTimeOffset Now { get; }
}
