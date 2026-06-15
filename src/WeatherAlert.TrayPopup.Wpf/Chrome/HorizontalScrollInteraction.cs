using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WeatherAlert.TrayPopup.Wpf.Chrome;

public static class HorizontalScrollInteraction
{
    private const double WheelPixelsPerNotch = 42;
    private const double WheelSmoothingTauMs = 72;
    private const double WheelStopThreshold = 0.35;

    private sealed class InteractionState
    {
        public bool IsDragging;
        public Point DragStartPoint;
        public double DragStartOffset;
        public double WheelTargetOffset;
        public bool WheelAnimationActive;
    }

    private static readonly HashSet<ScrollViewer> AnimatingScrollViewers = [];
    private static bool _renderingHooked;
    private static DateTime _lastRenderUtc = DateTime.UtcNow;

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(InteractionState),
        typeof(HorizontalScrollInteraction),
        new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(HorizontalScrollInteraction),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if (e.NewValue is true)
        {
            Attach(scrollViewer);
        }
        else
        {
            Detach(scrollViewer);
        }
    }

    public static void Attach(ScrollViewer scrollViewer)
    {
        scrollViewer.SetValue(StateProperty, new InteractionState());
        scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        scrollViewer.PanningMode = PanningMode.HorizontalOnly;
        scrollViewer.Cursor = Cursors.Hand;

        scrollViewer.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        scrollViewer.PreviewMouseMove += OnPreviewMouseMove;
        scrollViewer.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
        scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    private static void Detach(ScrollViewer scrollViewer)
    {
        scrollViewer.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        scrollViewer.PreviewMouseMove -= OnPreviewMouseMove;
        scrollViewer.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
        scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        AnimatingScrollViewers.Remove(scrollViewer);
        scrollViewer.ClearValue(StateProperty);
        UnhookRenderingIfIdle();
    }

    private static InteractionState GetState(ScrollViewer scrollViewer) =>
        (InteractionState)scrollViewer.GetValue(StateProperty);

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var state = GetState(scrollViewer);
        StopWheelAnimation(scrollViewer, state);
        state.IsDragging = true;
        state.DragStartPoint = e.GetPosition(scrollViewer);
        state.DragStartOffset = scrollViewer.HorizontalOffset;
        scrollViewer.CaptureMouse();
        scrollViewer.Cursor = Cursors.SizeWE;
        e.Handled = true;
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var state = GetState(scrollViewer);
        if (!state.IsDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPoint = e.GetPosition(scrollViewer);
        var delta = currentPoint.X - state.DragStartPoint.X;
        scrollViewer.ScrollToHorizontalOffset(state.DragStartOffset - delta);
        e.Handled = true;
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var state = GetState(scrollViewer);
        if (!state.IsDragging)
        {
            return;
        }

        state.IsDragging = false;
        if (scrollViewer.IsMouseCaptured)
        {
            scrollViewer.ReleaseMouseCapture();
        }

        scrollViewer.Cursor = Cursors.Hand;
        e.Handled = true;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var state = GetState(scrollViewer);
        if (state.IsDragging)
        {
            return;
        }

        if (!state.WheelAnimationActive)
        {
            state.WheelTargetOffset = scrollViewer.HorizontalOffset;
        }

        var scrollAmount = -(e.Delta / 120d) * WheelPixelsPerNotch;
        state.WheelTargetOffset = ClampOffset(scrollViewer, state.WheelTargetOffset + scrollAmount);
        StartWheelAnimation(scrollViewer, state);
        e.Handled = true;
    }

    private static void StartWheelAnimation(ScrollViewer scrollViewer, InteractionState state)
    {
        state.WheelAnimationActive = true;
        if (AnimatingScrollViewers.Add(scrollViewer))
        {
            HookRendering();
        }
    }

    private static void StopWheelAnimation(ScrollViewer scrollViewer, InteractionState state)
    {
        state.WheelAnimationActive = false;
        state.WheelTargetOffset = scrollViewer.HorizontalOffset;
        AnimatingScrollViewers.Remove(scrollViewer);
        UnhookRenderingIfIdle();
    }

    private static void HookRendering()
    {
        if (_renderingHooked)
        {
            return;
        }

        _lastRenderUtc = DateTime.UtcNow;
        CompositionTarget.Rendering += OnCompositionRendering;
        _renderingHooked = true;
    }

    private static void UnhookRenderingIfIdle()
    {
        if (_renderingHooked && AnimatingScrollViewers.Count == 0)
        {
            CompositionTarget.Rendering -= OnCompositionRendering;
            _renderingHooked = false;
        }
    }

    private static void OnCompositionRendering(object? sender, EventArgs e)
    {
        if (AnimatingScrollViewers.Count == 0)
        {
            UnhookRenderingIfIdle();
            return;
        }

        var now = DateTime.UtcNow;
        var deltaMs = Math.Clamp((now - _lastRenderUtc).TotalMilliseconds, 1, 32);
        _lastRenderUtc = now;
        var smoothingFactor = 1 - Math.Exp(-deltaMs / WheelSmoothingTauMs);

        var completed = new List<ScrollViewer>();
        foreach (var scrollViewer in AnimatingScrollViewers)
        {
            if (!scrollViewer.IsLoaded)
            {
                completed.Add(scrollViewer);
                continue;
            }

            var state = GetState(scrollViewer);
            if (!state.WheelAnimationActive || state.IsDragging)
            {
                completed.Add(scrollViewer);
                continue;
            }

            var current = scrollViewer.HorizontalOffset;
            var target = ClampOffset(scrollViewer, state.WheelTargetOffset);
            var delta = target - current;

            if (Math.Abs(delta) <= WheelStopThreshold)
            {
                scrollViewer.ScrollToHorizontalOffset(target);
                state.WheelAnimationActive = false;
                completed.Add(scrollViewer);
                continue;
            }

            scrollViewer.ScrollToHorizontalOffset(current + delta * smoothingFactor);
        }

        foreach (var scrollViewer in completed)
        {
            AnimatingScrollViewers.Remove(scrollViewer);
        }

        UnhookRenderingIfIdle();
    }

    private static double ClampOffset(ScrollViewer scrollViewer, double offset)
    {
        var maxOffset = Math.Max(0, scrollViewer.ScrollableWidth);
        return Math.Clamp(offset, 0, maxOffset);
    }
}
