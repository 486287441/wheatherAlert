using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class RainSummaryFormatterTests
{
    [Fact]
    public void FormatDayLine_WithMultipleRanges_JoinsAllRanges()
    {
        var summary = new DailyRainSummary(
            new DateOnly(2026, 6, 6),
            true,
            new[]
            {
                new RainTimeRange(new DateTimeOffset(2026, 6, 6, 7, 0, 0, TimeSpan.FromHours(8)), new DateTimeOffset(2026, 6, 6, 11, 0, 0, TimeSpan.FromHours(8))),
                new RainTimeRange(new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.FromHours(8)), new DateTimeOffset(2026, 6, 6, 16, 0, 0, TimeSpan.FromHours(8)))
            },
            "moderate");

        var text = RainSummaryFormatter.FormatDayLine("今天", summary);

        Assert.Equal("今天 07:00-11:00、12:00-16:00 有降雨（中雨）", text);
    }

    [Fact]
    public void FormatDayLine_WithRain_IncludesTimeRangesAndIntensity()
    {
        var summary = new DailyRainSummary(
            new DateOnly(2026, 5, 29),
            true,
            new[] { new RainTimeRange(new DateTimeOffset(2026, 5, 29, 11, 0, 0, TimeSpan.FromHours(8)), new DateTimeOffset(2026, 5, 29, 13, 0, 0, TimeSpan.FromHours(8))) },
            "moderate");

        var text = RainSummaryFormatter.FormatDayLine("今天", summary);

        Assert.Equal("今天 11:00-13:00 有降雨（中雨）", text);
    }

    [Fact]
    public void FormatTimeRanges_EndsAtMidnight_ShowsTwentyFourHundred()
    {
        var range = new RainTimeRange(
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.FromHours(8)),
            new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.FromHours(8)));

        var text = RainSummaryFormatter.FormatTimeRanges([range]);

        Assert.Equal("12:00-24:00", text);
    }

    [Fact]
    public void FormatBalloonBody_WithoutRain_ReturnsNoRainText()
    {
        var summary = new DailyRainSummary(
            new DateOnly(2026, 5, 29),
            false,
            Array.Empty<RainTimeRange>(),
            "none");

        Assert.Equal("无降雨", RainSummaryFormatter.FormatBalloonBody(summary));
    }
}
