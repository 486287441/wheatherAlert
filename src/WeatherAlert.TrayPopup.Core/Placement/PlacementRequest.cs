namespace WeatherAlert.TrayPopup.Core.Placement;

public readonly record struct PlacementRequest(
    ScreenRect WorkArea,
    FlyoutSize FlyoutSize,
    double Margin = 16);
