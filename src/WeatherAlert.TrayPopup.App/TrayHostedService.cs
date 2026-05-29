using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WeatherAlert.TrayPopup.App.Configuration;
using WeatherAlert.TrayPopup.App.Tray;
using WeatherAlert.TrayPopup.Core;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Wpf.Views;
using Application = System.Windows.Application;

namespace WeatherAlert.TrayPopup.App;

public sealed class TrayHostedService : IHostedService, IDisposable
{
    private static readonly IReadOnlyDictionary<string, string> CityMap = new Dictionary<string, string>
    {
        ["101010100"] = "北京",
        ["101020100"] = "上海",
        ["101280601"] = "深圳",
        ["101280101"] = "广州"
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<TrayHostedService> _logger;
    private readonly WeatherOptions _weatherOptions;
    private readonly TrayIconSet _trayIconSet;

    private Thread? _uiThread;
    private NotifyIcon? _notifyIcon;
    private Application? _wpfApp;
    private ManualResetEventSlim? _uiReady;

    public TrayHostedService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime applicationLifetime,
        ILogger<TrayHostedService> logger,
        IOptions<WeatherOptions> weatherOptions,
        TrayIconSet trayIconSet)
    {
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _weatherOptions = weatherOptions.Value;
        _trayIconSet = trayIconSet;
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

    private async Task ShowManualCheckNotificationsAsync(RainCheckResult result)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        if (!result.HasAnyRain)
        {
            _notifyIcon.ShowBalloonTip(4000, "WeatherAlert", "今天和明天均无降雨。", ToolTipIcon.Info);
            return;
        }

        ShowDayRainBalloon("今天", result.Today);
        await Task.Delay(4000);
        ShowDayRainBalloon("明天", result.Tomorrow);
    }

    private void ShowDayRainBalloon(string dayLabel, DailyRainSummary summary)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var icon = summary.HasRain ? ToolTipIcon.Warning : ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(
            5000,
            $"降雨提醒 · {dayLabel}",
            RainSummaryFormatter.FormatBalloonBody(summary),
            icon);
    }

    private async Task<RainCheckResult> RefreshTrayIconAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var checker = scope.ServiceProvider.GetRequiredService<IWeatherChecker>();
            var result = await checker.CheckAsync(CancellationToken.None);
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
                var window = new HistoryWindow(historyRepository);
                window.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open history window.");
                _notifyIcon?.ShowBalloonTip(3000, "WeatherAlert", "打开历史通知失败，请查看日志。", ToolTipIcon.Error);
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
            var appState = scope.ServiceProvider.GetRequiredService<IAppStateRepository>();
            var currentCityCode = await appState.GetValueAsync(AppStateKeys.CurrentCityCode, CancellationToken.None)
                                 ?? _weatherOptions.DefaultCityCode;

            string? selected = null;
            _wpfApp.Dispatcher.Invoke(() =>
            {
                var dialog = new CitySelectionDialog(CityMap, currentCityCode);
                if (dialog.ShowDialog() == true)
                {
                    selected = dialog.SelectedCityCode;
                }
            });

            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            await appState.SetValueAsync(AppStateKeys.CurrentCityCode, selected, CancellationToken.None);
            if (CityMap.TryGetValue(selected, out var cityName))
            {
                await appState.SetValueAsync(AppStateKeys.CurrentCityName, cityName, CancellationToken.None);
            }

            _notifyIcon?.ShowBalloonTip(
                3000,
                "WeatherAlert",
                $"城市已切换为 {CityMap.GetValueOrDefault(selected, selected)}",
                ToolTipIcon.Info);
            await RefreshTrayIconAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open city dialog.");
            _notifyIcon?.ShowBalloonTip(3000, "WeatherAlert", "切换城市失败，请查看日志。", ToolTipIcon.Error);
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
