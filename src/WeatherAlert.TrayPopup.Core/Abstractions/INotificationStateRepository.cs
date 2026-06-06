using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface INotificationStateRepository
{
    Task<bool> HasNotifiedAsync(
        string cityCode,
        DateOnly targetDate,
        RainDayPerspective perspective,
        CancellationToken cancellationToken);

    Task MarkNotifiedAsync(
        string cityCode,
        DateOnly targetDate,
        RainDayPerspective perspective,
        string messageHash,
        CancellationToken cancellationToken);
}
