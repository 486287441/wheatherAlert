using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Services;

public sealed class RainDetectionService : IRainDetectionService
{
    public RainCheckResult Detect(IReadOnlyList<HourlyForecast> hourlyForecasts, DateTimeOffset now)
    {
        var localNow = now.ToLocalTime();
        var today = DateOnly.FromDateTime(localNow.Date);
        var tomorrow = today.AddDays(1);

        var todaySummary = BuildDailySummary(today, hourlyForecasts);
        var tomorrowSummary = BuildDailySummary(tomorrow, hourlyForecasts);

        return new RainCheckResult(todaySummary, tomorrowSummary);
    }

    private static DailyRainSummary BuildDailySummary(DateOnly targetDate, IReadOnlyList<HourlyForecast> hourlyForecasts)
    {
        var dayItems = hourlyForecasts
            .Where(x => DateOnly.FromDateTime(x.ForecastTime.LocalDateTime.Date) == targetDate)
            .OrderBy(x => x.ForecastTime)
            .ToList();

        if (dayItems.Count == 0)
        {
            return new DailyRainSummary(targetDate, false, Array.Empty<RainTimeRange>(), "none");
        }

        var rainyItems = dayItems.Where(IsRainSignal).ToList();
        if (rainyItems.Count == 0)
        {
            return new DailyRainSummary(targetDate, false, Array.Empty<RainTimeRange>(), "none");
        }

        var ranges = BuildRainRanges(dayItems);
        var maxPrecipitation = rainyItems.Max(x => x.PrecipitationMm);
        var maxProbability = rainyItems.Max(x => x.PrecipitationProbability);

        return new DailyRainSummary(
            targetDate,
            true,
            ranges,
            GetIntensityLabel(maxPrecipitation, maxProbability));
    }

    private static bool IsRainSignal(HourlyForecast item)
    {
        if (item.PrecipitationMm > 0)
        {
            return true;
        }

        return item.PrecipitationProbability >= 40 && IndicatesRainInText(item.ConditionText);
    }

    private static bool IndicatesRainInText(string? conditionText)
    {
        if (string.IsNullOrWhiteSpace(conditionText))
        {
            return false;
        }

        return conditionText.Contains('雨', StringComparison.Ordinal)
            || conditionText.Contains("rain", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<RainTimeRange> BuildRainRanges(IReadOnlyList<HourlyForecast> dayItems)
    {
        var ranges = new List<RainTimeRange>();
        DateTimeOffset? rangeStart = null;
        DateTimeOffset? rangeEnd = null;

        foreach (var item in dayItems)
        {
            if (IsRainSignal(item))
            {
                rangeStart ??= item.ForecastTime;
                rangeEnd = item.ForecastTime.AddHours(1);
                continue;
            }

            if (rangeStart is not null && rangeEnd is not null)
            {
                ranges.Add(new RainTimeRange(rangeStart.Value, rangeEnd.Value));
                rangeStart = null;
                rangeEnd = null;
            }
        }

        if (rangeStart is not null && rangeEnd is not null)
        {
            ranges.Add(new RainTimeRange(rangeStart.Value, rangeEnd.Value));
        }

        return ranges;
    }

    private static string GetIntensityLabel(double maxPrecipitationMm, int maxProbability)
    {
        if (maxPrecipitationMm >= 10 || maxProbability >= 80)
        {
            return "heavy";
        }

        if (maxPrecipitationMm >= 2 || maxProbability >= 40)
        {
            return "moderate";
        }

        return "light";
    }
}
