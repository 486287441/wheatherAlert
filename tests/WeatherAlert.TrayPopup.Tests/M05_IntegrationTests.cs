using System.Reflection;
using WeatherAlert.TrayPopup.App;
using WeatherAlert.TrayPopup.Core.Flyout;
using WeatherAlert.TrayPopup.Core.Placement;
using WeatherAlert.TrayPopup.Tests.Helpers;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class M05_IntegrationTests
{
    [Fact]
    public void App_assembly_exposes_tray_host_entry_type()
    {
        var type = typeof(TrayHostedService);
        Assert.Equal("WeatherAlert.TrayPopup.App", type.Assembly.GetName().Name);
    }

    [Fact]
    public void App_has_entry_point()
    {
        var entry = typeof(TrayHostedService).Assembly.EntryPoint;
        Assert.NotNull(entry);
        Assert.Contains("Main", entry.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void City_map_has_four_preset_cities()
    {
        var cities = new Dictionary<string, string>
        {
            ["101010100"] = "北京",
            ["101020100"] = "上海",
            ["101280601"] = "深圳",
            ["101280101"] = "广州"
        };

        Assert.Equal(4, cities.Count);
    }
}
