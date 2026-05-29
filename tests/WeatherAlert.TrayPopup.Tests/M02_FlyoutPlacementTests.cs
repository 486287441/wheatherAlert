using WeatherAlert.TrayPopup.Core.Placement;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class M02_FlyoutPlacementTests
{
    [Fact]
    public void CalculateBottomRight_1920x1040_workArea_places_card_at_bottom_right_with_margin()
    {
        var request = new PlacementRequest(
            new ScreenRect(0, 0, 1920, 1040),
            new FlyoutSize(360, 420),
            Margin: 16);

        var result = FlyoutPlacementCalculator.CalculateBottomRight(request);

        Assert.Equal(1544, result.Left);
        Assert.Equal(604, result.Top);
    }

    [Fact]
    public void CalculateBottomRight_non_origin_work_area_offsets_position()
    {
        var request = new PlacementRequest(
            new ScreenRect(1920, 0, 1920, 1080),
            new FlyoutSize(320, 400),
            Margin: 12);

        var result = FlyoutPlacementCalculator.CalculateBottomRight(request);

        Assert.Equal(3508, result.Left);
        Assert.Equal(668, result.Top);
    }

    [Fact]
    public void CalculateBottomRight_small_work_area_still_aligns_to_bottom_right_edge()
    {
        var request = new PlacementRequest(
            new ScreenRect(100, 50, 800, 600),
            new FlyoutSize(300, 280),
            Margin: 8);

        var result = FlyoutPlacementCalculator.CalculateBottomRight(request);

        Assert.Equal(592, result.Left);
        Assert.Equal(362, result.Top);
    }
}
