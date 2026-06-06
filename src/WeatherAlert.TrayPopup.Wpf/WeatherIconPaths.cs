namespace WeatherAlert.TrayPopup.Wpf;

using System.IO;

public static class WeatherIconPaths
{
    public static string Resolve(string iconFileName)
        => Path.Combine(AppContext.BaseDirectory, "Assets", "weather-icons", iconFileName);
}
