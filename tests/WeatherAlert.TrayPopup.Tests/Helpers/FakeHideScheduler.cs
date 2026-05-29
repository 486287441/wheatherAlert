using WeatherAlert.TrayPopup.Core.Flyout;

namespace WeatherAlert.TrayPopup.Tests.Helpers;

public sealed class FakeHideScheduler : IFlyoutHideScheduler
{
    private Action? _pending;
    private TimeSpan _pendingDelay;

    public void Schedule(TimeSpan delay, Action onElapsed)
    {
        _pendingDelay = delay;
        _pending = onElapsed;
    }

    public void Cancel() => _pending = null;

    public TimeSpan PendingDelay => _pendingDelay;

    public bool HasPending => _pending is not null;

    public void Elapse()
    {
        _pending?.Invoke();
        _pending = null;
    }
}
