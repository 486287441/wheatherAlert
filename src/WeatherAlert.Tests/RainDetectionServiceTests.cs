using WeatherAlert.Core.Models;
using WeatherAlert.Core.Services;
using Xunit;

namespace WeatherAlert.Tests;

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
            new(new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.FromHours(8)), 0.1, 10, "Cloudy"),
            new(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.FromHours(8)), 0.8, 55, "Rain"),
            new(new DateTimeOffset(2026, 5, 28, 13, 0, 0, TimeSpan.FromHours(8)), 0.6, 50, "Rain")
        };

        var service = new RainDetectionService();
        var result = service.Detect(forecasts, now);

        Assert.True(result.Today.HasRain);
        Assert.Equal(2, result.Today.TimeRanges.Count);
        Assert.Equal("moderate", result.Today.IntensityLabel);
    }
}
