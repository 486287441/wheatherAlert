using System.Threading;
using System.Threading.Tasks;
using WeatherAlert.Core.Models;

namespace WeatherAlert.Core.Abstractions;

public interface IWeatherChecker
{
    Task<RainCheckResult> CheckAsync(CancellationToken cancellationToken);
}
