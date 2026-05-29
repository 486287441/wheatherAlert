using System.Windows;
using System.Windows.Forms;
using WeatherAlert.TrayPopup.Core.Placement;
using ScreenRect = WeatherAlert.TrayPopup.Core.Placement.ScreenRect;

namespace WeatherAlert.TrayPopup.App;

public sealed class SystemWorkAreaProvider : IWorkAreaProvider
{
    public ScreenRect GetPrimaryWorkArea()
    {
        var area = SystemInformation.WorkingArea;
        return new ScreenRect(area.Left, area.Top, area.Width, area.Height);
    }
}
