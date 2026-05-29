using WeatherAlert.TrayPopup.Infrastructure.Geo;
using Xunit;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class ChinaCityCatalogTests
{
    [Fact]
    public void GetGroups_LoadsCsvAndGroupsByProvince()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Data", "china-cities.csv");
        Assert.True(File.Exists(csvPath), $"Missing test data file: {csvPath}");

        var catalog = new ChinaCityCatalog();
        var groups = catalog.GetGroups();
        var all = catalog.GetAllCities();

        Assert.NotEmpty(groups);
        Assert.True(all.Count > 3000);
        Assert.Contains(groups, g => g.Province == "北京市");
        Assert.Contains(groups, g => g.Province == "广东省");
        Assert.All(all, c => Assert.False(string.IsNullOrWhiteSpace(c.Province)));
        Assert.NotNull(catalog.FindById("101010100"));
        Assert.Equal("北京", catalog.FindById("101010100")!.Name);
        Assert.Equal("北京市", catalog.FindById("101010100")!.Province);
    }
}
