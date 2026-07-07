using System.Globalization;
using System.Text.RegularExpressions;

namespace WeatherAlert.TrayPopup.Core.Models;

public static class NotificationHistoryFormatter
{
    private static readonly Regex DatePattern = new(@"\d{4}-\d{2}-\d{2}", RegexOptions.Compiled);
    private static readonly Regex LegacyIntensityPattern = new(
        @"\s*\?\?\s*(heavy|moderate|light)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TrailingEnglishIntensityPattern = new(
        @"[，,]\s*(heavy|moderate|light)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static HistoryDisplayRow ToDisplayRow(NotificationHistoryEntry entry, string? cityName)
        => new()
        {
            NotifiedAt = ClockTimeFormatter.FormatDateTime(entry.CreatedAt),
            Type = FormatType(entry.Type),
            City = string.IsNullOrWhiteSpace(cityName) ? entry.CityCode : cityName,
            Detail = FormatDetail(entry)
        };

    public static string FormatType(NotificationType type) => type switch
    {
        NotificationType.Rain => "降雨",
        NotificationType.Error => "错误",
        _ => type.ToString()
    };

    public static string FormatDetail(NotificationHistoryEntry entry)
        => entry.Type == NotificationType.Rain
            ? FormatRainDetail(entry)
            : entry.Body;

    public static string FormatRainHistoryBody(DailyRainSummary summary, DateTimeOffset referenceTime)
    {
        var periodText = RainSummaryFormatter.FormatTimeRanges(summary.TimeRanges);
        var intensity = RainSummaryFormatter.FormatIntensity(summary.IntensityLabel);
        return $"{FormatTargetDayLabel(summary.Date, referenceTime)} {periodText} 有降雨（{intensity}）";
    }

    public static string FormatRainHistoryTitle(DateOnly targetDate, DateTimeOffset referenceTime)
        => $"降雨提醒 · {FormatTargetDayLabel(targetDate, referenceTime)}";

    private static string FormatRainDetail(NotificationHistoryEntry entry)
    {
        var normalizedBody = NormalizeLegacyRainBody(entry.Body);
        var dayLabel = ExtractTargetDayLabel(entry.Title, entry.Body, entry.CreatedAt);
        if (string.IsNullOrWhiteSpace(dayLabel))
        {
            return normalizedBody;
        }

        if (normalizedBody.StartsWith(dayLabel, StringComparison.Ordinal))
        {
            return normalizedBody;
        }

        return $"{dayLabel} {normalizedBody}";
    }

    private static string NormalizeLegacyRainBody(string body)
    {
        var text = body.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        text = LegacyIntensityPattern.Replace(
            text,
            match => $" 有降雨（{RainSummaryFormatter.FormatIntensity(match.Groups[1].Value)}）");
        text = DatePattern.Replace(text, string.Empty, 1).TrimStart();
        text = TrailingEnglishIntensityPattern.Replace(
            text,
            match => $" 有降雨（{RainSummaryFormatter.FormatIntensity(match.Groups[1].Value)}）");

        if (!text.Contains("有降雨", StringComparison.Ordinal))
        {
            var commaIndex = text.LastIndexOf('，');
            if (commaIndex > 0 && Regex.IsMatch(text, @"\d{2}:\d{2}"))
            {
                var times = ClockTimeFormatter.NormalizeRangeText(text[..commaIndex].Trim());
                var intensity = text[(commaIndex + 1)..].Trim();
                return $"{times} 有降雨（{intensity}）";
            }

            if (Regex.IsMatch(text, @"\d{2}:\d{2}"))
            {
                return ClockTimeFormatter.NormalizeRangeText($"{text} 有降雨");
            }
        }

        return ClockTimeFormatter.NormalizeRangeText(text.Trim());
    }

    private static string? ExtractTargetDayLabel(string title, string body, DateTimeOffset createdAt)
    {
        const string titlePrefix = "降雨提醒 · ";
        string? label = null;
        if (title.StartsWith(titlePrefix, StringComparison.Ordinal))
        {
            label = title[titlePrefix.Length..].Trim();
        }

        if (label is "今天" or "明天")
        {
            var refToday = DateOnly.FromDateTime(createdAt.LocalDateTime);
            var targetDate = label == "今天" ? refToday : refToday.AddDays(1);
            return FormatTargetDayLabel(targetDate, createdAt);
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        if (title.Contains("今天", StringComparison.Ordinal) || body.Contains("今天", StringComparison.Ordinal))
        {
            return FormatTargetDayLabel(DateOnly.FromDateTime(createdAt.LocalDateTime), createdAt);
        }

        if (title.Contains("明天", StringComparison.Ordinal) || body.Contains("明天", StringComparison.Ordinal))
        {
            return FormatTargetDayLabel(DateOnly.FromDateTime(createdAt.LocalDateTime).AddDays(1), createdAt);
        }

        var match = DatePattern.Match($"{title} {body}");
        if (!match.Success || !DateOnly.TryParse(match.Value, out var parsedDate))
        {
            return null;
        }

        return FormatTargetDayLabel(parsedDate, createdAt);
    }

    public static string FormatTargetDayLabel(DateOnly targetDate, DateTimeOffset referenceTime)
    {
        var today = DateOnly.FromDateTime(referenceTime.LocalDateTime);
        if (targetDate == today)
        {
            return $"今天{FormatDateParenthetical(targetDate)}";
        }

        if (targetDate == today.AddDays(1))
        {
            return $"明天{FormatDateParenthetical(targetDate)}";
        }

        return targetDate.ToString("MM月dd日");
    }

    private static string FormatDateParenthetical(DateOnly date)
    {
        var weekday = CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(date.DayOfWeek);
        return $"（{date.Month}月{date.Day}号，{weekday}）";
    }
}
