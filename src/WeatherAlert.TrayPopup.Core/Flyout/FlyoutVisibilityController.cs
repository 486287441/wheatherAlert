namespace WeatherAlert.TrayPopup.Core.Flyout;

public sealed class FlyoutVisibilityController
{
    private readonly IFlyoutHideScheduler _hideScheduler;
    private readonly TimeSpan _hideDelay;
    private FlyoutVisibilityState _state = FlyoutVisibilityState.Hidden;

    public FlyoutVisibilityController(
        IFlyoutHideScheduler hideScheduler,
        TimeSpan? hideDelay = null)
    {
        _hideScheduler = hideScheduler;
        _hideDelay = hideDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public FlyoutVisibilityState State => _state;

    public event Action<FlyoutVisibilityState>? StateChanged;

    public void OnTrayClick()
    {
        if (_state == FlyoutVisibilityState.Visible)
        {
            HideImmediate();
            return;
        }

        Show();
    }

    public void OnPointerEnter()
    {
        _hideScheduler.Cancel();
    }

    public void OnPointerLeave()
    {
        if (_state != FlyoutVisibilityState.Visible)
        {
            return;
        }

        _hideScheduler.Schedule(_hideDelay, HideImmediate);
    }

    public void Show()
    {
        _hideScheduler.Cancel();
        SetState(FlyoutVisibilityState.Visible);
    }

    public void HideImmediate()
    {
        _hideScheduler.Cancel();
        SetState(FlyoutVisibilityState.Hidden);
    }

    private void SetState(FlyoutVisibilityState next)
    {
        if (_state == next)
        {
            return;
        }

        _state = next;
        StateChanged?.Invoke(_state);
    }
}
