using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using WeatherAlert.App.Configuration;
using WeatherAlert.Core.Abstractions;
using WeatherAlert.Core.Models;

namespace WeatherAlert.App.Tray;

public sealed class TrayMenuService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrayMenuService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IAppStateRepository _appStateRepository;
    private readonly IWeatherChecker _weatherChecker;
    private readonly WeatherOptions _options;
    private Thread? _uiThread;
    private NotifyIcon? _notifyIcon;
    private Icon? _rainTrayIcon;
    private Icon? _noRainTrayIcon;

    private const string RainTrayIconAbsolutePath = @"C:\Users\48628\.cursor\projects\d-code-wheatherAlert\assets\c__Users_48628_AppData_Roaming_Cursor_User_workspaceStorage_907bbf39874138ff64a170d24507cabb_images_73358373-41bd-438b-95c2-54ec7274c6d4-6ad52b00-457d-45b4-921d-afb131d9b1c0.png";
    private const string NoRainTrayIconAbsolutePath = @"C:\Users\48628\.cursor\projects\d-code-wheatherAlert\assets\c__Users_48628_AppData_Roaming_Cursor_User_workspaceStorage_907bbf39874138ff64a170d24507cabb_images_3551fd86-19a2-48de-8ac0-5d7afd083d43-ffb99285-3fd2-4e2f-93e9-0e8e9791b0fe.png";

    private static readonly IReadOnlyDictionary<string, string> CityMap = new Dictionary<string, string>
    {
        ["101010100"] = "北京",
        ["101020100"] = "上海",
        ["101280601"] = "深圳",
        ["101280101"] = "广州"
    };

    public TrayMenuService(
        IServiceProvider serviceProvider,
        ILogger<TrayMenuService> logger,
        IHostApplicationLifetime applicationLifetime,
        IAppStateRepository appStateRepository,
        IWeatherChecker weatherChecker,
        Microsoft.Extensions.Options.IOptions<WeatherOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _appStateRepository = appStateRepository;
        _weatherChecker = weatherChecker;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _uiThread = new Thread(RunTrayUi)
        {
            Name = "TrayUiThread",
            IsBackground = true
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        _rainTrayIcon?.Dispose();
        _rainTrayIcon = null;
        _noRainTrayIcon?.Dispose();
        _noRainTrayIcon = null;
        return Task.CompletedTask;
    }

    private void RunTrayUi()
    {
        _rainTrayIcon = LoadTrayIcon(RainTrayIconAbsolutePath);
        _noRainTrayIcon = LoadTrayIcon(NoRainTrayIconAbsolutePath);

        _notifyIcon = new NotifyIcon
        {
            Text = "WeatherAlert",
            Icon = _noRainTrayIcon ?? SystemIcons.Information,
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("立即检查", null, async (_, _) => await RunManualCheckAsync());
        menu.Items.Add("查看历史通知", null, (_, _) => ShowHistoryWindow());
        menu.Items.Add("切换城市", null, async (_, _) => await ShowCityDialogAsync());
        menu.Items.Add("-");
        menu.Items.Add("退出", null, (_, _) => _applicationLifetime.StopApplication());
        _notifyIcon.ContextMenuStrip = menu;

        _ = RefreshTrayIconAsync();
        Application.Run();
    }

    private async Task RunManualCheckAsync()
    {
        var result = await RefreshTrayIconAsync();
        var text = result.HasAnyRain ? "已完成检查：有降雨提醒。": "已完成检查：今天和明天无降雨提醒。";
        _notifyIcon?.ShowBalloonTip(3000, "WeatherAlert", text, ToolTipIcon.Info);
    }

    private async Task<RainCheckResult> RefreshTrayIconAsync()
    {
        var result = await _weatherChecker.CheckAsync(CancellationToken.None);
        ApplyTrayIconByRain(result);
        return result;
    }

    private void ApplyTrayIconByRain(RainCheckResult result)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var hasRainTodayOrTomorrow = result.Today.HasRain || result.Tomorrow.HasRain;
        _notifyIcon.Icon = hasRainTodayOrTomorrow
            ? _rainTrayIcon ?? SystemIcons.Warning
            : _noRainTrayIcon ?? SystemIcons.Information;
    }

    private Icon? LoadTrayIcon(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            _logger.LogWarning("Tray icon file not found: {ImagePath}", imagePath);
            return null;
        }

        try
        {
            using var bitmap = new Bitmap(imagePath);
            var hIcon = bitmap.GetHicon();
            try
            {
                using var icon = Icon.FromHandle(hIcon);
                return (Icon)icon.Clone();
            }
            finally
            {
                _ = DestroyIcon(hIcon);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tray icon from {ImagePath}", imagePath);
            return null;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private void ShowHistoryWindow()
    {
        LaunchWinUi("--history");
    }

    private async Task ShowCityDialogAsync()
    {
        LaunchWinUi("--city-select");
        await Task.CompletedTask;
    }

    private void LaunchWinUi(string mode)
    {
        var exeCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "WeatherAlert.WinUI.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\WeatherAlert.WinUI\bin\Debug\net10.0-windows10.0.19041.0\WeatherAlert.WinUI.exe")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\WeatherAlert.WinUI\bin\Release\net10.0-windows10.0.19041.0\win-x64\WeatherAlert.WinUI.exe"))
        };

        var exePath = exeCandidates.FirstOrDefault(File.Exists);
        if (exePath is null)
        {
            _notifyIcon?.ShowBalloonTip(3000, "WeatherAlert", "WinUI 模块未找到，请先构建 WeatherAlert.WinUI。", ToolTipIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = mode,
            UseShellExecute = true
        });
    }
}
