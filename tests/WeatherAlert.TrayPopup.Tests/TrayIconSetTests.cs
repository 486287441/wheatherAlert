using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class TrayIconSetTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void HasRainTodayOrTomorrow_matches_expected(bool todayRain, bool tomorrowRain, bool expected)
    {
        var result = new RainCheckResult(
            new DailyRainSummary(DateOnly.FromDateTime(DateTime.Now), todayRain, Array.Empty<RainTimeRange>(), "none"),
            new DailyRainSummary(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), tomorrowRain, Array.Empty<RainTimeRange>(), "none"));

        var actual = result.Today.HasRain || result.Tomorrow.HasRain;
        Assert.Equal(expected, actual);
    }
}
