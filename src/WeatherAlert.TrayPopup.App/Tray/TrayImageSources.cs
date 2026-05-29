using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.App.Tray;

public static class TrayImageSources
{
    public static ImageSource ForResult(RainCheckResult result)
    {
        var file = result.Today.HasRain || result.Tomorrow.HasRain
            ? "tray-rain.png"
            : "tray-clear.png";
        return Load(file);
    }

    public static ImageSource Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
