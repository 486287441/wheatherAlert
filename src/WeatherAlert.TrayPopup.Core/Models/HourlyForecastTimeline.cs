namespace WeatherAlert.TrayPopup.Core.Models;

public static class HourlyForecastTimeline
{
    public const int DefaultWindowHours = 48;

    public static (DateTimeOffset Start, DateTimeOffset End) GetTodayTomorrowWindow(DateTimeOffset now)
    {
        var localNow = now.ToLocalTime();
        var windowStart = new DateTimeOffset(localNow.Date, localNow.Offset);
        var windowEnd = windowStart.AddDays(2);
        return (windowStart, windowEnd);
    }

    public static IReadOnlyList<HourlyForecast> TakeTodayAndTomorrowHours(
        IReadOnlyList<HourlyForecast> forecasts,
        DateTimeOffset now)
        => MergeTodayAndTomorrowHours(forecasts, Array.Empty<HourlyForecast>(), now);

    public static IReadOnlyList<HourlyForecast> MergeTodayAndTomorrowHours(
        IReadOnlyList<HourlyForecast> liveForecasts,
        IReadOnlyList<HourlyForecast> cachedForecasts,
        DateTimeOffset now)
    {
        var (windowStart, windowEnd) = GetTodayTomorrowWindow(now);
        var merged = new Dictionary<DateTimeOffset, HourlyForecast>();

        foreach (var forecast in cachedForecasts)
        {
            if (forecast.ForecastTime >= windowStart && forecast.ForecastTime < windowEnd)
            {
                merged[NormalizeToHour(forecast.ForecastTime)] = forecast;
            }
        }

        foreach (var forecast in liveForecasts)
        {
            if (forecast.ForecastTime >= windowStart && forecast.ForecastTime < windowEnd)
            {
                merged[NormalizeToHour(forecast.ForecastTime)] = forecast;
            }
        }

        return merged.Values
            .OrderBy(item => item.ForecastTime)
            .ToList();
    }

    public static (IReadOnlyList<HourlyForecastRowSlot> Today, IReadOnlyList<HourlyForecastRowSlot> Tomorrow, int AvailableCount)
        BuildTodayTomorrowRows(
            IReadOnlyList<HourlyForecast> liveForecasts,
            IReadOnlyList<HourlyForecast> cachedForecasts,
            DateTimeOffset now)
    {
        var merged = MergeTodayAndTomorrowHours(liveForecasts, cachedForecasts, now);
        var mergedByHour = merged.ToDictionary(item => NormalizeToHour(item.ForecastTime));
        var (windowStart, _) = GetTodayTomorrowWindow(now);
        var tomorrowStart = windowStart.AddDays(1);

        var today = BuildDayRow(windowStart, mergedByHour);
        var tomorrow = BuildDayRow(tomorrowStart, mergedByHour);
        var availableCount = today.Count(slot => slot.Forecast is not null)
            + tomorrow.Count(slot => slot.Forecast is not null);

        return (today, tomorrow, availableCount);
    }

    public static string FormatDayRowLabel(DateOnly date, DateTimeOffset now)
    {
        var localToday = DateOnly.FromDateTime(now.ToLocalTime().Date);
        if (date == localToday)
        {
            return $"{date:MM/dd} \u4eca\u5929";
        }

        if (date == localToday.AddDays(1))
        {
            return $"{date:MM/dd} \u660e\u5929";
        }

        return date.ToString("MM/dd");
    }

    public static string FormatHourOnlyLabel(DateTimeOffset forecastTime)
        => ClockTimeFormatter.Format(forecastTime);

    public static string FormatHourLabel(DateTimeOffset forecastTime, DateTimeOffset now)
    {
        var localTime = forecastTime.ToLocalTime();
        var localNow = now.ToLocalTime();
        var timeText = ClockTimeFormatter.Format(forecastTime);

        if (localTime.Date == localNow.Date)
        {
            return timeText;
        }

        if (localTime.Date == localNow.Date.AddDays(1))
        {
            return $"\u660e\u5929 {timeText}";
        }

        return localTime.ToString("MM/dd HH:mm");
    }

    private static IReadOnlyList<HourlyForecastRowSlot> BuildDayRow(
        DateTimeOffset dayStart,
        IReadOnlyDictionary<DateTimeOffset, HourlyForecast> mergedByHour)
    {
        return Enumerable.Range(0, 24)
            .Select(hourOffset =>
            {
                var hour = dayStart.AddHours(hourOffset);
                mergedByHour.TryGetValue(hour, out var forecast);
                return new HourlyForecastRowSlot(hour, forecast);
            })
            .ToList();
    }

    private static DateTimeOffset NormalizeToHour(DateTimeOffset time)
    {
        var localTime = time.ToLocalTime();
        return new DateTimeOffset(
            localTime.Year,
            localTime.Month,
            localTime.Day,
            localTime.Hour,
            0,
            0,
            localTime.Offset);
    }
}
