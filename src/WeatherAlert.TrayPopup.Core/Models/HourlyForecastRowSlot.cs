namespace WeatherAlert.TrayPopup.Core.Models;

public sealed record HourlyForecastRowSlot(DateTimeOffset Hour, HourlyForecast? Forecast);
