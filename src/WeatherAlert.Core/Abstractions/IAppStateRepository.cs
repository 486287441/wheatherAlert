namespace WeatherAlert.Core.Abstractions;

public interface IAppStateRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);

    Task SetValueAsync(string key, string value, CancellationToken cancellationToken);
}
