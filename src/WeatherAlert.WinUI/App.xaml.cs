using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Options;
using WeatherAlert.Core.Abstractions;
using WeatherAlert.Infrastructure.Persistence;

namespace WeatherAlert.WinUI;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App()
    {
        InitializeComponent();
        _services = BuildServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mode = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault() ?? "--history";
        var window = new MainWindow(_services, mode);
        window.Activate();
    }

    private static IServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<SqliteOptions>()
            .Bind(configuration.GetSection(SqliteOptions.SectionName));
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<IAppStateRepository, AppStateRepository>();
        services.AddSingleton<INotificationHistoryRepository, NotificationHistoryRepository>();
        return services.BuildServiceProvider();
    }
}
