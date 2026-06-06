using WeatherAlert.TrayPopup.Core.Models;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class WeatherIconMapperTests
{
    [Theory]
    [InlineData("\u6674", 12, "sunny.png")]
    [InlineData("\u6674", 22, "clear-night.png")]
    [InlineData("\u591a\u4e91", 10, "partly-cloudy-day.png")]
    [InlineData("\u591a\u4e91", 22, "partly-cloudy-night.png")]
    [InlineData("\u9634", 15, "cloudy.png")]
    [InlineData("\u5c0f\u96e8", 8, "rain.png")]
    [InlineData("\u96f7\u9635\u96e8", 16, "thunderstorm-rain.png")]
    [InlineData("\u66b4\u96ea", 6, "snowy.png")]
    [InlineData("\u5927\u98ce", 14, "windy.png")]
    public void MapToIconFile_MapsKnownConditions(string text, int hour, string expected)
    {
        var time = new DateTimeOffset(2026, 6, 6, hour, 0, 0, TimeSpan.FromHours(8));
        var result = WeatherIconMapper.MapToIconFile(text, time);
        Assert.Equal(expected, result);
    }
}

public sealed class HourlyForecastTimelineTests
{
    [Fact]
    public void BuildTodayTomorrowRows_ReturnsTwoFullDayRows()
    {
        var offset = TimeSpan.FromHours(8);
        var now = new DateTimeOffset(2026, 6, 6, 15, 30, 0, offset);
        var live = new[]
        {
            new HourlyForecast(new DateTimeOffset(2026, 6, 6, 16, 0, 0, offset), 0.1, 10, "\u6674"),
            new HourlyForecast(new DateTimeOffset(2026, 6, 7, 8, 0, 0, offset), 0.5, 60, "\u5c0f\u96e8")
        };
        var cached = new[]
        {
            new HourlyForecast(new DateTimeOffset(2026, 6, 6, 8, 0, 0, offset), 0.2, 30, "\u591a\u4e91")
        };

        var (today, tomorrow, availableCount) = HourlyForecastTimeline.BuildTodayTomorrowRows(live, cached, now);

        Assert.Equal(24, today.Count);
        Assert.Equal(24, tomorrow.Count);
        Assert.Equal(3, availableCount);
        Assert.Null(today[0].Forecast);
        Assert.NotNull(today[8].Forecast);
        Assert.NotNull(today[16].Forecast);
        Assert.NotNull(tomorrow[8].Forecast);
    }

    [Fact]
    public void FormatDayRowLabel_UsesTodayAndTomorrowLabels()
    {
        var now = new DateTimeOffset(2026, 6, 6, 10, 0, 0, TimeSpan.FromHours(8));

        Assert.Equal("06/06 \u4eca\u5929", HourlyForecastTimeline.FormatDayRowLabel(new DateOnly(2026, 6, 6), now));
        Assert.Equal("06/07 \u660e\u5929", HourlyForecastTimeline.FormatDayRowLabel(new DateOnly(2026, 6, 7), now));
    }

    [Fact]
    public void MergeTodayAndTomorrowHours_PrefersLiveDataOverCache()
    {
        var offset = TimeSpan.FromHours(8);
        var now = new DateTimeOffset(2026, 6, 6, 15, 30, 0, offset);
        var hour = new DateTimeOffset(2026, 6, 6, 10, 0, 0, offset);
        var cached = new[]
        {
            new HourlyForecast(hour, 0.2, 30, "\u591a\u4e91"),
            new HourlyForecast(new DateTimeOffset(2026, 6, 6, 11, 0, 0, offset), 0.4, 50, "\u9634")
        };
        var live = new[]
        {
            new HourlyForecast(hour, 1.0, 80, "\u5927\u96e8"),
            new HourlyForecast(new DateTimeOffset(2026, 6, 6, 16, 0, 0, offset), 0.1, 10, "\u6674")
        };

        var result = HourlyForecastTimeline.MergeTodayAndTomorrowHours(live, cached, now);

        Assert.Equal(3, result.Count);
        Assert.Equal("\u5927\u96e8", result[0].ConditionText);
        Assert.Equal("\u9634", result[1].ConditionText);
        Assert.Equal("\u6674", result[2].ConditionText);
    }

    [Fact]
    public void TakeTodayAndTomorrowHours_ReturnsFullCalendarWindowRegardlessOfCurrentTime()
    {
        var offset = TimeSpan.FromHours(8);
        var now = new DateTimeOffset(2026, 6, 6, 15, 30, 0, offset);
        var forecasts = Enumerable.Range(0, 96)
            .Select(hour => new HourlyForecast(
                new DateTimeOffset(2026, 6, 5, 0, 0, 0, offset).AddHours(hour),
                0,
                0,
                "\u6674"))
            .ToList();

        var result = HourlyForecastTimeline.TakeTodayAndTomorrowHours(forecasts, now);

        Assert.Equal(48, result.Count);
        Assert.Equal(new DateTimeOffset(2026, 6, 6, 0, 0, 0, offset), result[0].ForecastTime);
        Assert.Equal(new DateTimeOffset(2026, 6, 7, 23, 0, 0, offset), result[^1].ForecastTime);
    }

    [Fact]
    public void GetTodayTomorrowWindow_AlwaysStartsAtTodayMidnight()
    {
        var now = new DateTimeOffset(2026, 6, 6, 22, 45, 0, TimeSpan.FromHours(8));
        var (start, end) = HourlyForecastTimeline.GetTodayTomorrowWindow(now);

        Assert.Equal(new DateTimeOffset(2026, 6, 6, 0, 0, 0, TimeSpan.FromHours(8)), start);
        Assert.Equal(new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.FromHours(8)), end);
    }

    [Fact]
    public void FormatHourLabel_UsesRelativeDayLabels()
    {
        var now = new DateTimeOffset(2026, 6, 6, 10, 0, 0, TimeSpan.FromHours(8));

        Assert.Equal("14:00", HourlyForecastTimeline.FormatHourLabel(now.AddHours(4), now));
        Assert.Equal("\u660e\u5929 08:00", HourlyForecastTimeline.FormatHourLabel(now.AddDays(1).AddHours(-2), now));
        Assert.Equal("06/08 08:00", HourlyForecastTimeline.FormatHourLabel(now.AddDays(2).AddHours(-2), now));
    }
}
