using System.Windows;
using WeatherAlert.TrayPopup.Wpf.Chrome;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public enum TrayMenuAction
{
    CheckNow,
    History,
    HourlyForecast,
    ChangeCity,
    Exit
}

public partial class FrostedTrayMenuWindow : Window
{
    public event EventHandler<TrayMenuAction>? ActionSelected;

    public FrostedTrayMenuWindow()
    {
        InitializeComponent();
        FrostedShell.Apply(this);
        PopupWindowHelper.EnableTrayPopupStyle(this);
    }

    private void RaiseAction(TrayMenuAction action)
    {
        ActionSelected?.Invoke(this, action);
        Hide();
    }

    private void OnCheckNowClick(object sender, RoutedEventArgs e) => RaiseAction(TrayMenuAction.CheckNow);

    private void OnHistoryClick(object sender, RoutedEventArgs e) => RaiseAction(TrayMenuAction.History);

    private void OnHourlyForecastClick(object sender, RoutedEventArgs e) => RaiseAction(TrayMenuAction.HourlyForecast);

    private void OnChangeCityClick(object sender, RoutedEventArgs e) => RaiseAction(TrayMenuAction.ChangeCity);

    private void OnExitClick(object sender, RoutedEventArgs e) => RaiseAction(TrayMenuAction.Exit);
}
