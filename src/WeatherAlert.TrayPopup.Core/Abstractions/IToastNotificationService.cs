namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IToastNotificationService
{
    void ShowInfo(string title, string body);

    void ShowWarning(string title, string body);

    void ShowError(string title, string body);
}
