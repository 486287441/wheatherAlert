namespace WeatherAlert.TrayPopup.Core.Models;

public static class RainNotificationStateKey
{
    public static string Format(DateOnly date, RainDayPerspective perspective)
        => $"{date:yyyy-MM-dd}#{ToKeyToken(perspective)}";

    public static string FormatLegacy(DateOnly date)
        => date.ToString("yyyy-MM-dd");

    private static string ToKeyToken(RainDayPerspective perspective) => perspective switch
    {
        RainDayPerspective.Today => "today",
        RainDayPerspective.Tomorrow => "tomorrow",
        _ => throw new ArgumentOutOfRangeException(nameof(perspective), perspective, null)
    };
}
