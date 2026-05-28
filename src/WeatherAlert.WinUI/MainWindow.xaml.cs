using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WeatherAlert.Core.Abstractions;

namespace WeatherAlert.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly string _mode;
    private readonly Dictionary<string, string> _cities = new()
    {
        ["101010100"] = "北京",
        ["101020100"] = "上海",
        ["101280601"] = "深圳",
        ["101280101"] = "广州"
    };

    public MainWindow(IServiceProvider services, string mode)
    {
        _services = services;
        _mode = mode;
        InitializeComponent();
        TrySetMicaBackdrop();
        _ = LoadAsync();
    }

    private void TrySetMicaBackdrop()
    {
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
    }

    private async Task LoadAsync()
    {
        if (_mode.Equals("--city-select", StringComparison.OrdinalIgnoreCase))
        {
            TitleBlock.Text = "切换城市";
            CityPanel.Visibility = Visibility.Visible;
            HistoryGrid.Visibility = Visibility.Collapsed;
            foreach (var city in _cities)
            {
                CityCombo.Items.Add(new CityItem(city.Key, city.Value));
            }

            var appStateRepository = _services.GetRequiredService<IAppStateRepository>();
            var current = await appStateRepository.GetValueAsync("current_city_code", CancellationToken.None) ?? "101010100";
            CityCombo.SelectedItem = CityCombo.Items.Cast<CityItem>().FirstOrDefault(x => x.Code == current);
            return;
        }

        TitleBlock.Text = "历史通知";
        var historyRepository = _services.GetRequiredService<INotificationHistoryRepository>();
        var history = await historyRepository.GetRecentAsync(100, CancellationToken.None);
        HistoryGrid.ItemsSource = history.Select(x => new
        {
            x.Title,
            x.Body,
            CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
    }

    private async void OnSaveCityClicked(object sender, RoutedEventArgs e)
    {
        var selected = CityCombo.SelectedItem as CityItem;
        if (selected is null)
        {
            return;
        }

        var appStateRepository = _services.GetRequiredService<IAppStateRepository>();
        await appStateRepository.SetValueAsync("current_city_code", selected.Code, CancellationToken.None);
        Close();
    }

    private sealed record CityItem(string Code, string Name)
    {
        public override string ToString() => $"{Name} ({Code})";
    }
}
