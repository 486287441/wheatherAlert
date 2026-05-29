namespace WeatherAlert.TrayPopup.Core.Placement;

public static class FlyoutPlacementCalculator
{
    public static PlacementResult CalculateBottomRight(PlacementRequest request)
    {
        var work = request.WorkArea;
        var size = request.FlyoutSize;
        var margin = request.Margin;

        var left = work.Left + work.Width - size.Width - margin;
        var top = work.Top + work.Height - size.Height - margin;
        return new PlacementResult(left, top);
    }
}
