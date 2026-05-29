using System.Windows;
using WeatherAlert.TrayPopup.Core.Placement;

namespace WeatherAlert.TrayPopup.Wpf.Chrome;

public static class FlyoutWindowChrome
{
    public static void ApplyPlacement(Window window, PlacementResult placement, FlyoutSize size)
    {
        window.Left = placement.Left;
        window.Top = placement.Top;
        window.Width = size.Width;
        window.Height = size.Height;
    }

    public static void ConfigureChrome(Window window, int cornerRadius = WindowBlurHelper.DefaultCornerRadius)
    {
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = false;
        window.Background = System.Windows.Media.Brushes.Transparent;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.Topmost = true;

        var chrome = new System.Windows.Shell.WindowChrome
        {
            CaptionHeight = 0,
            CornerRadius = new CornerRadius(cornerRadius),
            GlassFrameThickness = new Thickness(0),
            ResizeBorderThickness = new Thickness(0),
            UseAeroCaptionButtons = false
        };
        System.Windows.Shell.WindowChrome.SetWindowChrome(window, chrome);
    }
}
