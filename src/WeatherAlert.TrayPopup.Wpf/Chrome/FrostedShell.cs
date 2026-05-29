using System.Windows;

namespace WeatherAlert.TrayPopup.Wpf.Chrome;

public static class FrostedShell
{
    public static void Apply(Window window, int cornerRadius = WindowBlurHelper.DefaultCornerRadius)
    {
        FlyoutWindowChrome.ConfigureChrome(window, cornerRadius);
        window.SourceInitialized += OnSourceInitialized;
    }

    private static void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            _ = WindowBlurHelper.TryEnableBlur(window);
        }
    }
}
