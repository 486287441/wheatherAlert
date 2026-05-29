namespace WeatherAlert.TrayPopup.Infrastructure.Weather;

public static class RetryPolicyFactory
{
    public static IReadOnlyList<TimeSpan> CreateExponentialBackoff(int maxRetryCount)
    {
        var retries = Math.Max(1, maxRetryCount);
        var result = new List<TimeSpan>(retries);
        for (var i = 0; i < retries; i++)
        {
            result.Add(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }

        return result;
    }
}
