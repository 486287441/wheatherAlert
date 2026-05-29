using System.Windows;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public partial class HistoryWindow : Window
{
    private readonly INotificationHistoryRepository _historyRepository;
    private readonly ICityCatalog _cityCatalog;

    public HistoryWindow(INotificationHistoryRepository historyRepository, ICityCatalog cityCatalog)
    {
        _historyRepository = historyRepository;
        _cityCatalog = cityCatalog;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var rows = await _historyRepository.GetRecentAsync(100, CancellationToken.None);
        HistoryGrid.ItemsSource = rows
            .Select(entry =>
            {
                var cityName = _cityCatalog.FindById(entry.CityCode)?.DisplayName;
                return NotificationHistoryFormatter.ToDisplayRow(entry, cityName);
            })
            .ToList();
    }
}
