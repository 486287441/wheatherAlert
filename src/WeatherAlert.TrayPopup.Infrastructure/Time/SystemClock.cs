using WeatherAlert.TrayPopup.Core.Abstractions;

namespace WeatherAlert.TrayPopup.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
