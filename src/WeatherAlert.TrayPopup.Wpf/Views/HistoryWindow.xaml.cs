using System.Windows;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Wpf.Chrome;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public partial class HistoryWindow : Window
{
    private readonly INotificationHistoryRepository _historyRepository;

    public HistoryWindow(INotificationHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
        InitializeComponent();
        FrostedShell.Apply(this);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var rows = await _historyRepository.GetRecentAsync(100, CancellationToken.None);
        HistoryGrid.ItemsSource = rows.Select(x => new
        {
            CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            Type = x.Type.ToString(),
            x.CityCode,
            x.Title,
            x.Body
        }).ToList();
    }
}
