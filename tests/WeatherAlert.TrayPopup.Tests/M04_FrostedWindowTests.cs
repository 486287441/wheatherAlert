using System.Windows;
using WeatherAlert.TrayPopup.Core.Placement;
using WeatherAlert.TrayPopup.Tests.Helpers;
using WeatherAlert.TrayPopup.Wpf.Chrome;
using WeatherAlert.TrayPopup.Wpf.Views;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class M04_FrostedWindowTests
{
    [Fact]
    public void TryEnableBlur_with_zero_handle_returns_false()
    {
        Assert.False(WindowBlurHelper.TryEnableBlur(IntPtr.Zero));
    }

    [Fact]
    public void Default_chrome_constants_match_spec()
    {
        Assert.Equal(16, WindowBlurHelper.DefaultCornerRadius);
        Assert.Equal(360, WindowBlurHelper.DefaultFlyoutWidth);
        Assert.Equal(420, WindowBlurHelper.DefaultFlyoutHeight);
    }

    [Fact]
    public void FlyoutWindowChrome_apply_placement_sets_window_bounds()
    {
        StaTest.Run(() =>
        {
            var window = new Window();
            var placement = new PlacementResult(100, 200);
            var size = new FlyoutSize(360, 420);

            FlyoutWindowChrome.ApplyPlacement(window, placement, size);

            Assert.Equal(100, window.Left);
            Assert.Equal(200, window.Top);
            Assert.Equal(360, window.Width);
            Assert.Equal(420, window.Height);
        });
    }

    [Fact]
    public void FrostedFlyoutWindow_configures_borderless_chrome()
    {
        StaTest.Run(() =>
        {
            var window = new FrostedFlyoutWindow();

            Assert.Equal(WindowStyle.None, window.WindowStyle);
            Assert.False(window.AllowsTransparency);
            Assert.False(window.ShowInTaskbar);
            Assert.True(window.Topmost);
            Assert.NotNull(System.Windows.Shell.WindowChrome.GetWindowChrome(window));
        });
    }

}
