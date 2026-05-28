using WeatherAlert.Core.Models;

namespace WeatherAlert.Core.Abstractions;

public interface INotificationHistoryRepository
{
    Task AddAsync(NotificationHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}
