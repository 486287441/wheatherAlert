namespace WeatherAlert.Core.Abstractions;

public interface INotificationStateRepository
{
    Task<bool> HasNotifiedAsync(string cityCode, DateOnly targetDate, CancellationToken cancellationToken);

    Task MarkNotifiedAsync(
        string cityCode,
        DateOnly targetDate,
        string messageHash,
        CancellationToken cancellationToken);
}
