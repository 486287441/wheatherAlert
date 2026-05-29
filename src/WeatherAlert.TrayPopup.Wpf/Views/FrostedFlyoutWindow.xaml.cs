using System.Windows;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Wpf.Chrome;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public partial class FrostedFlyoutWindow : Window
{
    public event EventHandler? PointerEntered;
    public event EventHandler? PointerLeft;

    public FrostedFlyoutWindow()
    {
        InitializeComponent();
        FrostedShell.Apply(this);
        PopupWindowHelper.EnableTrayPopupStyle(this);
    }

    private void OnCardMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) =>
        PointerEntered?.Invoke(this, EventArgs.Empty);

    private void OnCardMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) =>
        PointerLeft?.Invoke(this, EventArgs.Empty);

    public void UpdateWeatherSummary(string cityName, string cityCode, RainCheckResult result)
    {
        TodayText.Text = FormatDay("今天", result.Today);
        TomorrowText.Text = FormatDay("明天", result.Tomorrow);
        StatusText.Text = $"{cityName} ({cityCode}) · 鼠标移开将自动收起";
    }

    private static string FormatDay(string label, DailyRainSummary summary)
        => RainSummaryFormatter.FormatDayLine(label, summary);
}
