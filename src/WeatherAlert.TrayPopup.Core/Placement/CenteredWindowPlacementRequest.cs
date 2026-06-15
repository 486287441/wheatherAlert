namespace WeatherAlert.TrayPopup.Core.Placement;

public readonly record struct CenteredWindowPlacementRequest(
    ScreenRect WorkArea,
    double DesiredWidth,
    double DesiredHeight,
    double MinWidth = 640,
    double MinHeight = 480,
    double MaxWidthRatio = 0.92,
    double MaxHeightRatio = 0.88,
    double ScreenMargin = 24);
