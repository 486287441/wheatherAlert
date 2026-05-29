using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Infrastructure.Geo;

public sealed class ChinaCityCatalog : ICityCatalog
{
    private readonly Lazy<CatalogData> _data;

    public ChinaCityCatalog()
    {
        _data = new Lazy<CatalogData>(LoadCatalog, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<CityCatalogGroup> GetGroups() => _data.Value.Groups;

    public IReadOnlyList<ChinaCityEntry> GetAllCities() => _data.Value.AllCities;

    public ChinaCityEntry? FindById(string cityId)
    {
        if (string.IsNullOrWhiteSpace(cityId))
        {
            return null;
        }

        return _data.Value.ById.GetValueOrDefault(cityId);
    }

    private static CatalogData LoadCatalog()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Data", "china-cities.csv");
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("China city catalog file was not found.", csvPath);
        }

        var cities = new List<ChinaCityEntry>();
        foreach (var line in File.ReadLines(csvPath))
        {
            if (string.IsNullOrWhiteSpace(line)
                || line.StartsWith("China-City-List", StringComparison.Ordinal)
                || line.StartsWith("Location_ID", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 10)
            {
                continue;
            }

            var id = parts[0].Trim();
            var nameZh = parts[2].Trim();
            var province = NullIfEmpty(parts[7]) ?? "其他";
            var admin2 = NullIfEmpty(parts[9]);
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nameZh))
            {
                continue;
            }

            cities.Add(new ChinaCityEntry(id, nameZh, province, admin2));
        }

        var byId = cities.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var groups = cities
            .GroupBy(x => x.Province)
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(g => new CityCatalogGroup(
                g.Key,
                g.OrderBy(c => c.Name, StringComparer.Ordinal).ToList()))
            .ToList();

        return new CatalogData(cities, byId, groups);
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CatalogData(
        IReadOnlyList<ChinaCityEntry> AllCities,
        Dictionary<string, ChinaCityEntry> ById,
        IReadOnlyList<CityCatalogGroup> Groups);
}
