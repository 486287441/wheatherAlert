using System.Text.RegularExpressions;

namespace WeatherAlert.TrayPopup.Core.Models;

public static class ClockTimeFormatter
{
    private static readonly Regex MidnightEndPattern = new(
        @"(?<start>\d{2}:\d{2})-00:00",
        RegexOptions.Compiled);

    public static string Format(DateTimeOffset time)
        => time.ToLocalTime().ToString("HH:mm");

    public static string FormatRange(DateTimeOffset start, DateTimeOffset end)
    {
        var localStart = start.ToLocalTime();
        var localEnd = end.ToLocalTime();
        var endText = localEnd.TimeOfDay == TimeSpan.Zero && localEnd.Date > localStart.Date
            ? "24:00"
            : Format(localEnd);

        return $"{Format(localStart)}-{endText}";
    }

    public static string FormatTimeRange(RainTimeRange range)
        => FormatRange(range.Start, range.End);

    public static string FormatTimeRanges(IReadOnlyList<RainTimeRange> timeRanges)
        => string.Join("\u3001", timeRanges.Select(FormatTimeRange));

    public static string FormatDateTime(DateTimeOffset time)
        => time.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public static string NormalizeRangeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return MidnightEndPattern.Replace(text, match =>
        {
            var start = match.Groups["start"].Value;
            return start == "00:00" ? match.Value : $"{start}-24:00";
        });
    }
}
