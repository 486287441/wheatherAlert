using System.Windows;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public partial class CitySelectionDialog : Window
{
    public CitySelectionDialog(IReadOnlyDictionary<string, string> cityMap, string currentCityCode)
    {
        InitializeComponent();
        CityCombo.ItemsSource = cityMap
            .Select(x => new CityItem(x.Key, x.Value))
            .ToList();
        CityCombo.SelectedValue = cityMap.ContainsKey(currentCityCode)
            ? currentCityCode
            : cityMap.Keys.FirstOrDefault();
    }

    public string? SelectedCityCode => CityCombo.SelectedValue as string;

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private sealed record CityItem(string Code, string Name)
    {
        public string Display => $"{Name} ({Code})";
    }
}
