using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class ClockTimeFormatterTests
{
    [Fact]
    public void FormatRange_EndsAtMidnight_UsesTwentyFourHundred()
    {
        var text = ClockTimeFormatter.FormatRange(
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.FromHours(8)),
            new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.FromHours(8)));

        Assert.Equal("12:00-24:00", text);
    }

    [Fact]
    public void FormatRange_StartsAtMidnight_KeepsZeroZero()
    {
        var text = ClockTimeFormatter.FormatRange(
            new DateTimeOffset(2026, 6, 6, 0, 0, 0, TimeSpan.FromHours(8)),
            new DateTimeOffset(2026, 6, 6, 6, 0, 0, TimeSpan.FromHours(8)));

        Assert.Equal("00:00-06:00", text);
    }

    [Fact]
    public void NormalizeRangeText_ConvertsMidnightEndToTwentyFourHundred()
    {
        Assert.Equal(
            "12:00-24:00 ???????",
            ClockTimeFormatter.NormalizeRangeText("12:00-00:00 ???????"));
    }

    [Fact]
    public void NormalizeRangeText_PreservesMidnightStartRange()
    {
        Assert.Equal(
            "00:00-14:00 ???????",
            ClockTimeFormatter.NormalizeRangeText("00:00-14:00 ???????"));
    }

    [Fact]
    public void FormatDateTime_UsesTwentyFourHourClock()
    {
        var text = ClockTimeFormatter.FormatDateTime(
            new DateTimeOffset(2026, 6, 6, 15, 30, 0, TimeSpan.FromHours(8)));

        Assert.Equal("2026-06-06 15:30", text);
    }
}
