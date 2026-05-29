namespace WeatherAlert.TrayPopup.Core.Models;

public static class RainSummaryFormatter
{
    public static string FormatDayLine(string dayLabel, DailyRainSummary summary)
    {
        if (!summary.HasRain)
        {
            return $"{dayLabel}：无降雨";
        }

        var ranges = FormatTimeRanges(summary.TimeRanges);
        return $"{dayLabel} {ranges} 有降雨（{FormatIntensity(summary.IntensityLabel)}）";
    }

    public static string FormatBalloonBody(DailyRainSummary summary)
    {
        if (!summary.HasRain)
        {
            return "无降雨";
        }

        return $"{FormatTimeRanges(summary.TimeRanges)} 有降雨（{FormatIntensity(summary.IntensityLabel)}）";
    }

    public static string FormatTimeRanges(IReadOnlyList<RainTimeRange> timeRanges)
        => string.Join("、", timeRanges.Select(r => $"{r.Start:HH:mm}-{r.End:HH:mm}"));

    public static string FormatIntensity(string intensityLabel) => intensityLabel switch
    {
        "heavy" => "大雨",
        "moderate" => "中雨",
        "light" => "小雨",
        _ => "降雨"
    };
}
