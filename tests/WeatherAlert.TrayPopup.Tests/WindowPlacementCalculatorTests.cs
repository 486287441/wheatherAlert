using WeatherAlert.TrayPopup.Core.Placement;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class WindowPlacementCalculatorTests
{
    [Fact]
    public void CalculateCenterFit_wide_desired_size_stays_within_work_area_and_centers()
    {
        var request = new CenteredWindowPlacementRequest(
            new ScreenRect(0, 0, 1920, 1040),
            DesiredWidth: 2280,
            DesiredHeight: 720);

        var bounds = WindowPlacementCalculator.CalculateCenterFit(request);

        Assert.Equal(1766.4, bounds.Width, precision: 1);
        Assert.Equal(720, bounds.Height);
        Assert.Equal(76.8, bounds.Left, precision: 1);
        Assert.Equal(160, bounds.Top, precision: 1);
        Assert.True(bounds.Left >= 0);
    }

    [Fact]
    public void CalculateCenterFit_laptop_work_area_does_not_place_window_off_screen()
    {
        var request = new CenteredWindowPlacementRequest(
            new ScreenRect(0, 0, 1366, 728),
            DesiredWidth: 2100,
            DesiredHeight: 660,
            MinWidth: 720);

        var bounds = WindowPlacementCalculator.CalculateCenterFit(request);

        Assert.True(bounds.Width <= 1366 - 48);
        Assert.True(bounds.Left >= 0);
        Assert.True(bounds.Top >= 0);
        Assert.True(bounds.Left + bounds.Width <= 1366);
        Assert.True(bounds.Top + bounds.Height <= 728);
    }

    [Fact]
    public void CalculateCenterFit_content_fits_uses_full_desired_size()
    {
        var request = new CenteredWindowPlacementRequest(
            new ScreenRect(0, 0, 2560, 1400),
            DesiredWidth: 2096,
            DesiredHeight: 560);

        var bounds = WindowPlacementCalculator.CalculateCenterFit(request);

        Assert.Equal(2096, bounds.Width);
        Assert.Equal(560, bounds.Height);
        Assert.Equal(232, bounds.Left);
        Assert.Equal(420, bounds.Top);
    }

    [Fact]
    public void CalculateCenterFit_non_origin_work_area_offsets_position()
    {
        var request = new CenteredWindowPlacementRequest(
            new ScreenRect(1920, 0, 1920, 1080),
            DesiredWidth: 1200,
            DesiredHeight: 700);

        var bounds = WindowPlacementCalculator.CalculateCenterFit(request);

        Assert.Equal(1200, bounds.Width);
        Assert.Equal(700, bounds.Height);
        Assert.Equal(2280, bounds.Left);
        Assert.Equal(190, bounds.Top);
    }
}
