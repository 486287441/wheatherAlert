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

        var ranges = MergeContiguousRanges(rainyItems);
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
        return item.PrecipitationMm > 0 || item.PrecipitationProbability > 0;
    }

    private static IReadOnlyList<RainTimeRange> MergeContiguousRanges(IReadOnlyList<HourlyForecast> rainyItems)
    {
        if (rainyItems.Count == 0)
        {
            return Array.Empty<RainTimeRange>();
        }

        var result = new List<RainTimeRange>();
        var start = rainyItems[0].ForecastTime;
        var previous = rainyItems[0].ForecastTime;

        for (var i = 1; i < rainyItems.Count; i++)
        {
            var current = rainyItems[i].ForecastTime;
            if (current - previous > TimeSpan.FromHours(1))
            {
                result.Add(new RainTimeRange(start, previous.AddHours(1)));
                start = current;
            }

            previous = current;
        }

        result.Add(new RainTimeRange(start, previous.AddHours(1)));
        return result;
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
