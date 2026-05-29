namespace WeatherAlert.TrayPopup.Core.Flyout;

public sealed class NoOpHideScheduler : IFlyoutHideScheduler
{
    public static readonly NoOpHideScheduler Instance = new();

    private NoOpHideScheduler()
    {
    }

    public void Schedule(TimeSpan delay, Action onElapsed)
    {
    }

    public void Cancel()
    {
    }
}
