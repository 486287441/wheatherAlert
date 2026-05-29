using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class RainSummaryFormatterTests
{
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
