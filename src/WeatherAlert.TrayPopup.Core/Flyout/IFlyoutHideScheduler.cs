namespace WeatherAlert.TrayPopup.Core.Flyout;

public interface IFlyoutHideScheduler
{
    void Schedule(TimeSpan delay, Action onElapsed);

    void Cancel();
}
