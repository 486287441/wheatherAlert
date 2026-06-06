using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using WeatherAlert.TrayPopup.Core.Abstractions;
using Windows.UI.Notifications;

namespace WeatherAlert.TrayPopup.App.Notifications;

public sealed class WindowsToastNotificationService : IToastNotificationService
{
    private readonly ILogger<WindowsToastNotificationService> _logger;
    private readonly Uri? _logoUri;

    public WindowsToastNotificationService(ILogger<WindowsToastNotificationService> logger)
    {
        _logger = logger;
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray-rain.png");
        if (File.Exists(logoPath))
        {
            _logoUri = new Uri(logoPath);
        }
    }

    public void ShowInfo(string title, string body) => Show(title, body, ToastScenario.Default);

    public void ShowWarning(string title, string body) => Show(title, body, ToastScenario.Reminder);

    public void ShowError(string title, string body) => Show(title, body, ToastScenario.Default);

    private void Show(string title, string body, ToastScenario scenario)
    {
        try
        {
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .SetToastScenario(scenario);

            if (_logoUri is not null)
            {
                builder.AddAppLogoOverride(_logoUri);
            }

            builder.Show(toast =>
            {
                toast.ExpirationTime = DateTimeOffset.Now.AddDays(7);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show Windows toast notification.");
        }
    }
}
