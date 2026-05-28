using WeatherAlert.Core.Abstractions;

namespace WeatherAlert.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
