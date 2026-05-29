using System.Threading;
using System.Threading.Tasks;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Core.Abstractions;

public interface IWeatherChecker
{
    Task<RainCheckResult> CheckAsync(CancellationToken cancellationToken);
}
