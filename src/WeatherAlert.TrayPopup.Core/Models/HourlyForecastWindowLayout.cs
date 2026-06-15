namespace WeatherAlert.TrayPopup.Core.Models;

/// <summary>
/// Layout constants aligned with HourlyForecastWindow.xaml (hour slot width, margins, row heights).
/// Used to estimate ideal window size when WPF has not finished measuring yet.
/// </summary>
public static class HourlyForecastWindowLayout
{
    public const double HourSlotWidth = 84;
    public const int HoursPerDay = 24;
    public const double WindowContentMargin = 16;
    public const double CardPadding = 16;
    public const double HeaderBlockHeight = 56;
    public const double DayLabelHeight = 30;
    public const double ForecastRowHeight = 118;
    public const double DaySectionGap = 12;
    public const double FooterHeight = 52;

    public static (double Width, double Height) GetIdealContentSize()
    {
        var width = HoursPerDay * HourSlotWidth + CardPadding * 2;
        var height = CardPadding * 2
            + HeaderBlockHeight
            + DayLabelHeight
            + ForecastRowHeight
            + DaySectionGap
            + DayLabelHeight
            + ForecastRowHeight
            + FooterHeight;

        return (width, height);
    }
}
