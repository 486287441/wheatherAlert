using Serilog;
using WeatherAlert.App.Configuration;
using WeatherAlert.App.Services;
using WeatherAlert.App.Tray;
using WeatherAlert.Core.Abstractions;
using WeatherAlert.Core.Services;
using WeatherAlert.Infrastructure.Persistence;
using WeatherAlert.Infrastructure.Time;
using WeatherAlert.Infrastructure.Weather;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "WEATHER_ALERT_");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services
    .AddOptions<WeatherOptions>()
    .Bind(builder.Configuration.GetSection(WeatherOptions.SectionName));
builder.Services
    .AddOptions<HeWeatherClientOptions>()
    .Bind(builder.Configuration.GetSection(HeWeatherClientOptions.SectionName));
builder.Services
    .AddOptions<SqliteOptions>()
    .Bind(builder.Configuration.GetSection(SqliteOptions.SectionName));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IRainDetectionService, RainDetectionService>();
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<IAppStateRepository, AppStateRepository>();
builder.Services.AddSingleton<INotificationStateRepository, NotificationStateRepository>();
builder.Services.AddSingleton<INotificationHistoryRepository, NotificationHistoryRepository>();
builder.Services.AddHttpClient<IWeatherApiClient, HeWeatherClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WeatherOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip
        | System.Net.DecompressionMethods.Deflate
        | System.Net.DecompressionMethods.Brotli
});
builder.Services.AddSingleton<IWeatherChecker, WeatherChecker>();
builder.Services.AddHostedService<SqliteSchemaInitializer>();
builder.Services.AddHostedService<ConfigValidationHostedService>();
builder.Services.AddHostedService<TrayMenuService>();
builder.Services.AddHostedService<WeatherWorker>();

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
startupLogger.LogInformation("WeatherAlert background service is starting.");

if (args.Any(a => string.Equals(a, "--check-now", StringComparison.OrdinalIgnoreCase)))
{
    var checker = app.Services.GetRequiredService<IWeatherChecker>();
    startupLogger.LogInformation("Manual check command received (--check-now).");
    await checker.CheckAsync(CancellationToken.None);
}

await app.RunAsync();
