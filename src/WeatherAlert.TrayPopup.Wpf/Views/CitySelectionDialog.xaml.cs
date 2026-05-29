using System.Windows;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Wpf.ViewModels;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public partial class CitySelectionDialog : Window
{
    private readonly CitySelectionViewModel _viewModel;

    public CitySelectionDialog(ICityCatalog catalog, ICityLocationService cityLocation, string? currentCityCode)
    {
        InitializeComponent();
        _viewModel = new CitySelectionViewModel(catalog, cityLocation);
        DataContext = _viewModel;
        Loaded += OnLoaded;
        _currentCityCode = currentCityCode;
    }

    private readonly string? _currentCityCode;

    public string? SelectedCityCode => _viewModel.SelectedCity?.Id;

    public string? SelectedCityName => _viewModel.SelectedCity?.DisplayName;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync(_currentCityCode, CancellationToken.None);
    }

    private async void OnRefreshLocationClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshLocatedCityAsync(CancellationToken.None);
    }

    private void OnUseLocatedCityClick(object sender, RoutedEventArgs e)
    {
        _viewModel.UseLocatedCity();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCity is null)
        {
            MessageBox.Show(this, "请选择一个城市，或点击「使用定位」。", "切换城市", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }
}
