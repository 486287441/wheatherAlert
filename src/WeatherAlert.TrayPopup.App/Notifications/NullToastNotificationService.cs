using WeatherAlert.TrayPopup.Core.Abstractions;

namespace WeatherAlert.TrayPopup.App.Notifications;

public sealed class NullToastNotificationService : IToastNotificationService
{
    public void ShowInfo(string title, string body)
    {
    }

    public void ShowWarning(string title, string body)
    {
    }

    public void ShowError(string title, string body)
    {
    }
}
