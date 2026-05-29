namespace WeatherAlert.TrayPopup.Core.Flyout;

public sealed class TaskDelayHideScheduler : IFlyoutHideScheduler, IDisposable
{
    private CancellationTokenSource? _cts;

    public void Schedule(TimeSpan delay, Action onElapsed)
    {
        Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = RunAsync(delay, onElapsed, token);
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private static async Task RunAsync(TimeSpan delay, Action onElapsed, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token).ConfigureAwait(false);
            onElapsed();
        }
        catch (OperationCanceledException)
        {
            // expected when pointer re-enters before hide
        }
    }

    public void Dispose() => Cancel();
}
