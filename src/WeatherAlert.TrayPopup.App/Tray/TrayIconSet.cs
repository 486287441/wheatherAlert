using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.App.Tray;

public sealed class TrayIconSet : IDisposable
{
    private readonly Icon? _rainIcon;
    private readonly Icon? _clearIcon;
    private readonly ILogger<TrayIconSet>? _logger;

    public TrayIconSet(ILogger<TrayIconSet> logger)
    {
        _logger = logger;
        _rainIcon = LoadIcon("tray-rain.png");
        _clearIcon = LoadIcon("tray-clear.png");
    }

    public Icon GetIconForResult(RainCheckResult result)
    {
        var hasRain = result.Today.HasRain || result.Tomorrow.HasRain;
        return hasRain
            ? _rainIcon ?? SystemIcons.Warning
            : _clearIcon ?? SystemIcons.Information;
    }

    public void Dispose()
    {
        _rainIcon?.Dispose();
        _clearIcon?.Dispose();
    }

    private Icon? LoadIcon(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(path))
        {
            _logger?.LogWarning("Tray icon not found: {Path}", path);
            return null;
        }

        try
        {
            using var bitmap = new Bitmap(path);
            var handle = bitmap.GetHicon();
            try
            {
                using var icon = Icon.FromHandle(handle);
                return (Icon)icon.Clone();
            }
            finally
            {
                _ = DestroyIcon(handle);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load tray icon from {Path}", path);
            return null;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
