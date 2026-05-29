using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class NotificationHistoryFormatterTests
{
    [Fact]
    public void ToDisplayRow_LegacyRainBody_NormalizesQuestionMarksAndIntensity()
    {
        var createdAt = new DateTimeOffset(2026, 5, 29, 13, 16, 0, TimeSpan.FromHours(8));
        var entry = new NotificationHistoryEntry(
            1,
            createdAt,
            NotificationType.Rain,
            "101280101",
            "降雨提醒 2026-05-30",
            "2026-05-30 00:00-14:00 ?? moderate",
            "{}");

        var row = NotificationHistoryFormatter.ToDisplayRow(entry, "广州");

        Assert.Equal("降雨", row.Type);
        Assert.Equal("广州", row.City);
        Assert.Equal("明天 00:00-14:00 有降雨（中雨）", row.Detail);
    }

    [Fact]
    public void ToDisplayRow_NewRainBody_ShowsReadableDetail()
    {
        var createdAt = new DateTimeOffset(2026, 5, 29, 13, 16, 0, TimeSpan.FromHours(8));
        var entry = new NotificationHistoryEntry(
            1,
            createdAt,
            NotificationType.Rain,
            "101280101",
            "降雨提醒 · 明天",
            "00:00-14:00 有降雨（中雨）",
            "{}");

        var row = NotificationHistoryFormatter.ToDisplayRow(entry, "广州");

        Assert.Equal("明天 00:00-14:00 有降雨（中雨）", row.Detail);
    }
}
