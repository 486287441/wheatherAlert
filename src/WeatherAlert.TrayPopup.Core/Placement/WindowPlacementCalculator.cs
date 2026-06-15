namespace WeatherAlert.TrayPopup.Core.Placement;

public static class WindowPlacementCalculator
{
    public static WindowBounds CalculateCenterFit(CenteredWindowPlacementRequest request)
    {
        var work = request.WorkArea;
        var margin = request.ScreenMargin;

        var availableWidth = Math.Max(0, work.Width - margin * 2);
        var availableHeight = Math.Max(0, work.Height - margin * 2);

        var maxWidth = Math.Min(availableWidth, work.Width * request.MaxWidthRatio);
        var maxHeight = Math.Min(availableHeight, work.Height * request.MaxHeightRatio);

        var width = Clamp(request.DesiredWidth, Math.Min(request.MinWidth, availableWidth), maxWidth);
        var height = Clamp(request.DesiredHeight, Math.Min(request.MinHeight, availableHeight), maxHeight);

        width = Math.Min(width, availableWidth);
        height = Math.Min(height, availableHeight);

        var left = work.Left + (work.Width - width) / 2;
        var top = work.Top + (work.Height - height) / 2;

        left = Clamp(left, work.Left + margin, work.Left + work.Width - width - margin);
        top = Clamp(top, work.Top + margin, work.Top + work.Height - height - margin);

        return new WindowBounds(left, top, width, height);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return max;
        }

        return Math.Clamp(value, min, max);
    }
}
