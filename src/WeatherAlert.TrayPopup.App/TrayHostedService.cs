using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.App.Configuration;
using WeatherAlert.TrayPopup.App.Notifications;
using WeatherAlert.TrayPopup.App.Tray;
using WeatherAlert.TrayPopup.Core;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Wpf.Views;
using Application = System.Windows.Application;

namespace WeatherAlert.TrayPopup.App;

public sealed class TrayHostedService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<TrayHostedService> _logger;
    private readonly WeatherOptions _weatherOptions;
    private readonly TrayIconSet _trayIconSet;
    private readonly IToastNotificationService _toastNotificationService;

    private Thread? _uiThread;
    private NotifyIcon? _notifyIcon;
    private Application? _wpfApp;
    private ManualResetEventSlim? _uiReady;

    public TrayHostedService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime applicationLifetime,
        ILogger<TrayHostedService> logger,
        IOptions<WeatherOptions> weatherOptions,
        TrayIconSet trayIconSet,
        IToastNotificationService toastNotificationService)
    {
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _weatherOptions = weatherOptions.Value;
        _trayIconSet = trayIconSet;
        _toastNotificationService = toastNotificationService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _uiReady = new ManualResetEventSlim(false);
        _uiThread = new Thread(RunStaLoop)
        {
            Name = "TrayUiThread",
            IsBackground = true
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _uiReady.Wait(TimeSpan.FromSeconds(10), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_wpfApp is not null)
        {
            try
            {
                _wpfApp.Dispatcher.Invoke(() => _wpfApp.Shutdown());
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Tray UI shutdown skipped.");
            }
        }

        _uiThread?.Join(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _uiReady?.Dispose();
    }

    private void RunStaLoop()
    {
        _wpfApp = new Application
        {
            // Tray app has no main window; only close via menu "退出" or host shutdown.
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        _notifyIcon = new NotifyIcon
        {
            Text = "WeatherAlert",
            Icon = _trayIconSet.GetIconForResult(EmptyRainResult()),
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("立即检查", null, async (_, _) => await RunManualCheckAsync());
        menu.Items.Add("查看历史通知", null, (_, _) => ShowHistoryWindow());
        menu.Items.Add("切换城市", null, async (_, _) => await ShowCityDialogAsync());
        menu.Items.Add("-");
        menu.Items.Add("退出", null, (_, _) => _applicationLifetime.StopApplication());
        _notifyIcon.ContextMenuStrip = menu;

        _uiReady?.Set();
        _ = RefreshTrayIconAsync();

        _wpfApp.Run();

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private async Task RunManualCheckAsync()
    {
        var result = await RefreshTrayIconAsync();
        await ShowManualCheckNotificationsAsync(result);
    }

    private Task ShowManualCheckNotificationsAsync(RainCheckResult result)
    {
        if (!result.HasAnyRain)
        {
            _toastNotificationService.ShowInfo("WeatherAlert", "今天和明天均无降雨。");
            return Task.CompletedTask;
        }

        ShowDayRainToast("今天", result.Today);
        ShowDayRainToast("明天", result.Tomorrow);
        return Task.CompletedTask;
    }

    private void ShowDayRainToast(string dayLabel, DailyRainSummary summary)
    {
        var title = $"降雨提醒 · {dayLabel}";
        var body = RainSummaryFormatter.FormatBalloonBody(summary);
        if (summary.HasRain)
        {
            _toastNotificationService.ShowWarning(title, body);
        }
        else
        {
            _toastNotificationService.ShowInfo(title, body);
        }
    }

    private async Task<RainCheckResult> RefreshTrayIconAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var checker = scope.ServiceProvider.GetRequiredService<IWeatherChecker>();
            var result = await checker.CheckAsync(CancellationToken.None, showToastNotifications: false);
            ApplyTrayIconByRain(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tray icon.");
            return EmptyRainResult();
        }
    }

    private void ApplyTrayIconByRain(RainCheckResult result)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Icon = _trayIconSet.GetIconForResult(result);
    }

    private void ShowHistoryWindow()
    {
        if (_wpfApp is null)
        {
            return;
        }

        // BeginInvoke avoids deadlocking the tray menu thread (WinForms callback + WPF Invoke).
        _wpfApp.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var historyRepository = scope.ServiceProvider.GetRequiredService<INotificationHistoryRepository>();
                var cityCatalog = scope.ServiceProvider.GetRequiredService<ICityCatalog>();
                var window = new HistoryWindow(historyRepository, cityCatalog);
                window.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open history window.");
                _toastNotificationService.ShowError("WeatherAlert", "打开历史通知失败，请查看日志。");
            }
        });
    }

    private async Task ShowCityDialogAsync()
    {
        if (_wpfApp is null)
        {
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cityLocation = scope.ServiceProvider.GetRequiredService<ICityLocationService>();
            var cityCatalog = scope.ServiceProvider.GetRequiredService<ICityCatalog>();
            var current = await cityLocation.GetCurrentCityAsync(CancellationToken.None);
            var currentCityCode = current?.Code ?? _weatherOptions.DefaultCityCode;

            string? selectedCode = null;
            string? selectedName = null;
            _wpfApp.Dispatcher.Invoke(() =>
            {
                var dialog = new CitySelectionDialog(cityCatalog, cityLocation, currentCityCode);
                if (dialog.ShowDialog() == true)
                {
                    selectedCode = dialog.SelectedCityCode;
                    selectedName = dialog.SelectedCityName;
                }
            });

            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedName))
            {
                await cityLocation.SetCurrentCityAsync(
                    new GeoCity(selectedCode, selectedName, null, null, "中国"),
                    CancellationToken.None);
            }
            else
            {
                var entry = cityCatalog.FindById(selectedCode);
                if (entry is not null)
                {
                    await cityLocation.SetCurrentCityAsync(entry, CancellationToken.None);
                }
            }

            var displayName = selectedName
                                ?? cityCatalog.FindById(selectedCode)?.DisplayName
                                ?? selectedCode;
            _toastNotificationService.ShowInfo("WeatherAlert", $"城市已切换为 {displayName}");
            await RefreshTrayIconAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open city dialog.");
            _toastNotificationService.ShowError("WeatherAlert", "切换城市失败，请查看日志。");
        }
    }

    private static RainCheckResult EmptyRainResult()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return new RainCheckResult(
            new DailyRainSummary(today, false, Array.Empty<RainTimeRange>(), "none"),
            new DailyRainSummary(today.AddDays(1), false, Array.Empty<RainTimeRange>(), "none"));
    }
}
