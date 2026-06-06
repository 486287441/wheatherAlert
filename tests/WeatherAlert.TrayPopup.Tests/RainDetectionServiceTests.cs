using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Core.Services;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class RainDetectionServiceTests
{
    [Fact]
    public void Detect_CrossDayBoundary_SplitsTodayAndTomorrow()
    {
        var now = new DateTimeOffset(2026, 5, 28, 23, 30, 0, TimeSpan.FromHours(8));
        var forecasts = new List<HourlyForecast>
        {
            new(new DateTimeOffset(2026, 5, 28, 23, 0, 0, TimeSpan.FromHours(8)), 0.2, 35, "Light rain"),
            new(new DateTimeOffset(2026, 5, 29, 1, 0, 0, TimeSpan.FromHours(8)), 1.0, 60, "Rain"),
        };

        var service = new RainDetectionService();
        var result = service.Detect(forecasts, now);

        Assert.True(result.Today.HasRain);
        Assert.True(result.Tomorrow.HasRain);
        Assert.Equal(new DateOnly(2026, 5, 28), result.Today.Date);
        Assert.Equal(new DateOnly(2026, 5, 29), result.Tomorrow.Date);
    }

    [Fact]
    public void Detect_EmptyData_ReturnsNoRainForBothDays()
    {
        var now = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.FromHours(8));
        var service = new RainDetectionService();

        var result = service.Detect(Array.Empty<HourlyForecast>(), now);

        Assert.False(result.Today.HasRain);
        Assert.False(result.Tomorrow.HasRain);
        Assert.Equal("none", result.Today.IntensityLabel);
        Assert.Equal("none", result.Tomorrow.IntensityLabel);
    }

    [Fact]
    public void Detect_IntermittentRain_MergesOnlyContiguousHours()
    {
        var now = new DateTimeOffset(2026, 5, 28, 9, 0, 0, TimeSpan.FromHours(8));
        var forecasts = new List<HourlyForecast>
        {
            new(new DateTimeOffset(2026, 5, 28, 9, 0, 0, TimeSpan.FromHours(8)), 0.3, 30, "Light rain"),
            new(new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.FromHours(8)), 0, 10, "Cloudy"),
            new(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.FromHours(8)), 0.8, 55, "Rain"),
            new(new DateTimeOffset(2026, 5, 28, 13, 0, 0, TimeSpan.FromHours(8)), 0.6, 50, "Rain")
        };

        var service = new RainDetectionService();
        var result = service.Detect(forecasts, now);

        Assert.True(result.Today.HasRain);
        Assert.Equal(2, result.Today.TimeRanges.Count);
        Assert.Equal(new DateTimeOffset(2026, 5, 28, 9, 0, 0, TimeSpan.FromHours(8)), result.Today.TimeRanges[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.FromHours(8)), result.Today.TimeRanges[0].End);
        Assert.Equal(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.FromHours(8)), result.Today.TimeRanges[1].Start);
        Assert.Equal(new DateTimeOffset(2026, 5, 28, 14, 0, 0, TimeSpan.FromHours(8)), result.Today.TimeRanges[1].End);
        Assert.Equal("moderate", result.Today.IntensityLabel);
    }

    [Fact]
    public void Detect_MorningAndAfternoonRain_ReturnsTwoSeparateRanges()
    {
        var now = new DateTimeOffset(2026, 6, 6, 6, 0, 0, TimeSpan.FromHours(8));
        var forecasts = new List<HourlyForecast>
        {
            new(new DateTimeOffset(2026, 6, 6, 7, 0, 0, TimeSpan.FromHours(8)), 0.8, 60, "小雨"),
            new(new DateTimeOffset(2026, 6, 6, 8, 0, 0, TimeSpan.FromHours(8)), 1.0, 65, "小雨"),
            new(new DateTimeOffset(2026, 6, 6, 9, 0, 0, TimeSpan.FromHours(8)), 0.9, 60, "小雨"),
            new(new DateTimeOffset(2026, 6, 6, 10, 0, 0, TimeSpan.FromHours(8)), 0.5, 55, "小雨"),
            new(new DateTimeOffset(2026, 6, 6, 11, 0, 0, TimeSpan.FromHours(8)), 0, 20, "多云"),
            new(new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.FromHours(8)), 1.2, 70, "中雨"),
            new(new DateTimeOffset(2026, 6, 6, 13, 0, 0, TimeSpan.FromHours(8)), 1.0, 68, "中雨"),
            new(new DateTimeOffset(2026, 6, 6, 14, 0, 0, TimeSpan.FromHours(8)), 0.8, 62, "小雨"),
            new(new DateTimeOffset(2026, 6, 6, 15, 0, 0, TimeSpan.FromHours(8)), 0.4, 50, "小雨")
        };

        var service = new RainDetectionService();
        var result = service.Detect(forecasts, now);

        Assert.True(result.Today.HasRain);
        Assert.Equal(2, result.Today.TimeRanges.Count);
        Assert.Equal("07:00-11:00", RainSummaryFormatter.FormatTimeRanges([result.Today.TimeRanges[0]]));
        Assert.Equal("12:00-16:00", RainSummaryFormatter.FormatTimeRanges([result.Today.TimeRanges[1]]));
    }
}
