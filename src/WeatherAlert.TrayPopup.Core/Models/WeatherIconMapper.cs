namespace WeatherAlert.TrayPopup.Core.Models;

public static class WeatherIconMapper
{
    public static string MapToIconFile(string? conditionText, DateTimeOffset forecastTime)
    {
        var isNight = IsNightHour(forecastTime.ToLocalTime().Hour);
        var text = conditionText ?? string.Empty;

        if (ContainsAny(text, "\u96f7", "Thunder"))
        {
            return ContainsAny(text, "\u96e8", "Rain") ? "thunderstorm-rain.png" : "thunderstorm.png";
        }

        if (ContainsAny(text, "\u96ea", "Snow"))
        {
            return "snowy.png";
        }

        if (ContainsAny(text, "\u96e8", "Rain", "Drizzle", "\u9635\u96e8", "\u5c0f\u96e8", "\u4e2d\u96e8", "\u5927\u96e8"))
        {
            return "rain.png";
        }

        if (ContainsAny(text, "\u98ce", "Wind"))
        {
            return "windy.png";
        }

        if (ContainsAny(text, "\u9634", "\u973e", "\u96fe", "Overcast", "Fog", "Haze", "\u6d6e\u5c18", "\u626c\u7802", "\u6c99"))
        {
            return "cloudy.png";
        }

        if (ContainsAny(text, "\u4e91", "Cloud"))
        {
            return isNight ? "partly-cloudy-night.png" : "partly-cloudy-day.png";
        }

        if (ContainsAny(text, "\u6674", "Clear", "Sunny"))
        {
            return isNight ? "clear-night.png" : "sunny.png";
        }

        return isNight ? "partly-cloudy-night.png" : "partly-cloudy-day.png";
    }

    public static bool IsNightHour(int localHour)
        => localHour is >= 19 or < 6;

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
