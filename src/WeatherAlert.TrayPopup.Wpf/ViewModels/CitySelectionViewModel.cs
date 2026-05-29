using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Wpf.ViewModels;

public sealed class CitySelectionViewModel : INotifyPropertyChanged
{
    private readonly ICityCatalog _catalog;
    private readonly ICityLocationService _cityLocation;
    private string _searchText = string.Empty;
    private string _locatedCityText = "正在获取定位…";
    private bool _isLocating;
    private ChinaCityEntry? _selectedCity;
    private GeoCity? _locatedCity;

    public CitySelectionViewModel(ICityCatalog catalog, ICityLocationService cityLocation)
    {
        _catalog = catalog;
        _cityLocation = cityLocation;
        CitiesView = CollectionViewSource.GetDefaultView(new ObservableCollection<ChinaCityEntry>(catalog.GetAllCities()));
        CitiesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ChinaCityEntry.GroupKey)));
        CitiesView.Filter = FilterCity;
    }

    public ICollectionView CitiesView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            CitiesView.Refresh();
        }
    }

    public string LocatedCityText
    {
        get => _locatedCityText;
        private set
        {
            if (_locatedCityText == value)
            {
                return;
            }

            _locatedCityText = value;
            OnPropertyChanged();
        }
    }

    public bool IsLocating
    {
        get => _isLocating;
        private set
        {
            if (_isLocating == value)
            {
                return;
            }

            _isLocating = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRefreshLocation));
        }
    }

    public bool CanRefreshLocation => !IsLocating;

    public ChinaCityEntry? SelectedCity
    {
        get => _selectedCity;
        set
        {
            if (ReferenceEquals(_selectedCity, value))
            {
                return;
            }

            _selectedCity = value;
            OnPropertyChanged();
        }
    }

    public GeoCity? LocatedCity => _locatedCity;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync(string? currentCityCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(currentCityCode))
        {
            SelectedCity = _catalog.FindById(currentCityCode);
        }

        await RefreshLocatedCityAsync(cancellationToken).ConfigureAwait(true);
    }

    public async Task RefreshLocatedCityAsync(CancellationToken cancellationToken)
    {
        IsLocating = true;
        LocatedCityText = "正在获取定位…";
        try
        {
            var city = await _cityLocation.TryDetectLocatedCityAsync(cancellationToken).ConfigureAwait(true);
            _locatedCity = city;
            if (city is null)
            {
                LocatedCityText = "定位失败，请在系统设置中允许位置权限后重试。";
                return;
            }

            await _cityLocation.PersistLocatedCityAsync(city, cancellationToken).ConfigureAwait(true);
            LocatedCityText = city.DisplayName;
        }
        catch
        {
            LocatedCityText = "定位失败，请稍后重试。";
            _locatedCity = null;
        }
        finally
        {
            IsLocating = false;
        }
    }

    public void UseLocatedCity()
    {
        if (_locatedCity is null)
        {
            return;
        }

        SelectedCity = _catalog.FindById(_locatedCity.Id)
            ?? new ChinaCityEntry(
                _locatedCity.Id,
                _locatedCity.Name,
                _locatedCity.Admin1 ?? "其他",
                _locatedCity.Admin2);
    }

    private bool FilterCity(object item)
    {
        if (item is not ChinaCityEntry city)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var keyword = SearchText.Trim();
        return city.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || city.Province.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || city.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || city.Id.Contains(keyword, StringComparison.Ordinal);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
