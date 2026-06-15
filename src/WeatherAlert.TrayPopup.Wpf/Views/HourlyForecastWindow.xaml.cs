using System.IO;
using System.Windows;
using System.Windows.Threading;
using WeatherAlert.TrayPopup.Core.Abstractions;
using WeatherAlert.TrayPopup.Core.Models;
using WeatherAlert.TrayPopup.Core.Placement;
using WeatherAlert.TrayPopup.Wpf.ViewModels;

namespace WeatherAlert.TrayPopup.Wpf.Views;

public partial class HourlyForecastWindow : Window
{
    private readonly IWeatherApiClient _weatherApiClient;
    private readonly IHourlyForecastCacheRepository _hourlyForecastCacheRepository;
    private readonly ICityLocationService _cityLocationService;
    private readonly ICityCatalog _cityCatalog;
    private readonly IClock _clock;

    public HourlyForecastWindow(
        IWeatherApiClient weatherApiClient,
        IHourlyForecastCacheRepository hourlyForecastCacheRepository,
        ICityLocationService cityLocationService,
        ICityCatalog cityCatalog,
        IClock clock)
    {
        _weatherApiClient = weatherApiClient;
        _hourlyForecastCacheRepository = hourlyForecastCacheRepository;
        _cityLocationService = cityLocationService;
        _cityCatalog = cityCatalog;
        _clock = clock;
        InitializeComponent();
        TitleText.Text = "\u0034\u0038\u5c0f\u65f6\u5929\u6c14\u9884\u62a5";
        SubtitleText.Text = "\u52a0\u8f7d\u4e2d\u2026";
        EmptyText.Text = "\u6682\u65e0\u9010\u5c0f\u65f6\u9884\u62a5\u6570\u636e\u3002";
        CloseButton.Content = "\u5173\u95ed";
        Loaded += OnLoaded;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        try
        {
            var city = await _cityLocationService.GetCurrentCityAsync(CancellationToken.None);
            var cityCode = city?.Code;
            var cityName = city?.Name;

            if (string.IsNullOrWhiteSpace(cityCode))
            {
                cityCode = _cityCatalog.FindById("101010100")?.Id ?? "101010100";
                cityName ??= _cityCatalog.FindById(cityCode)?.DisplayName ?? cityCode;
            }

            cityName ??= _cityCatalog.FindById(cityCode)?.DisplayName ?? cityCode;
            var title = $"\u0034\u0038\u5c0f\u65f6\u5929\u6c14\u9884\u62a5 \u00b7 {cityName}";
            Title = title;
            TitleText.Text = title;

            var hourly = await _weatherApiClient.GetHourlyForecastAsync(cityCode, CancellationToken.None);
            await _hourlyForecastCacheRepository.UpsertAsync(cityCode, hourly, _clock.Now, CancellationToken.None);
            var (windowStart, windowEnd) = HourlyForecastTimeline.GetTodayTomorrowWindow(_clock.Now);
            var cached = await _hourlyForecastCacheRepository.GetRangeAsync(
                cityCode,
                windowStart,
                windowEnd,
                CancellationToken.None);
            var (todayRows, tomorrowRows, availableCount) = HourlyForecastTimeline.BuildTodayTomorrowRows(
                hourly,
                cached,
                _clock.Now);

            var localToday = DateOnly.FromDateTime(_clock.Now.ToLocalTime().Date);
            TodayDayLabel.Text = HourlyForecastTimeline.FormatDayRowLabel(localToday, _clock.Now);
            TomorrowDayLabel.Text = HourlyForecastTimeline.FormatDayRowLabel(localToday.AddDays(1), _clock.Now);
            TodayItems.ItemsSource = todayRows.Select(ToViewModel).ToList();
            TomorrowItems.ItemsSource = tomorrowRows.Select(ToViewModel).ToList();

            SubtitleText.Text = availableCount >= HourlyForecastTimeline.DefaultWindowHours
                ? "\u4eca\u5929\u3001\u660e\u5929\u5404\u4e00\u884c\uff0c\u6bcf\u884c 24 \u5c0f\u65f6\uff0c\u53ef\u62d6\u62fd\u6216\u6eda\u8f6e\u6a2a\u5411\u67e5\u770b"
                : $"\u4eca\u5929\u3001\u660e\u5929\u5404\u4e00\u884c\uff0c\u5f53\u524d\u53ef\u7528 {availableCount}/{HourlyForecastTimeline.DefaultWindowHours} \u5c0f\u65f6\uff08\u7f3a\u5931\u5c0f\u65f6\u4f1a\u968f\u672c\u5730\u7f13\u5b58\u8865\u5168\uff09";
            EmptyText.Visibility = availableCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            ScheduleWindowPlacement();
        }
        catch (Exception ex)
        {
            SubtitleText.Text = "\u52a0\u8f7d\u5931\u8d25";
            EmptyText.Text = ex.Message;
            EmptyText.Visibility = Visibility.Visible;
        }
    }

    private void ScheduleWindowPlacement()
    {
        Dispatcher.BeginInvoke(ApplyWindowPlacement, DispatcherPriority.Loaded);
    }

    private void ApplyWindowPlacement()
    {
        ContentHost.UpdateLayout();
        ContentHost.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var measured = ContentHost.DesiredSize;
        var fallback = HourlyForecastWindowLayout.GetIdealContentSize();
        var contentWidth = measured.Width > 1 ? measured.Width : fallback.Width;
        var contentHeight = measured.Height > 1 ? measured.Height : fallback.Height;

        var chromeWidth = HourlyForecastWindowLayout.WindowContentMargin * 2
            + SystemParameters.WindowResizeBorderThickness.Left
            + SystemParameters.WindowResizeBorderThickness.Right;
        var chromeHeight = HourlyForecastWindowLayout.WindowContentMargin * 2
            + SystemParameters.WindowCaptionHeight
            + SystemParameters.WindowResizeBorderThickness.Top
            + SystemParameters.WindowResizeBorderThickness.Bottom;

        var workArea = SystemParameters.WorkArea;
        var bounds = WindowPlacementCalculator.CalculateCenterFit(new CenteredWindowPlacementRequest(
            new ScreenRect(workArea.Left, workArea.Top, workArea.Width, workArea.Height),
            contentWidth + chromeWidth,
            contentHeight + chromeHeight,
            MinWidth: 720,
            MinHeight: 480));

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        TodayScrollViewer.ScrollToHorizontalOffset(0);
        TomorrowScrollViewer.ScrollToHorizontalOffset(0);
    }

    private static HourlyForecastItemViewModel ToViewModel(HourlyForecastRowSlot slot)
    {
        if (slot.Forecast is null)
        {
            return new HourlyForecastItemViewModel
            {
                HasData = false,
                TimeLabel = HourlyForecastTimeline.FormatHourOnlyLabel(slot.Hour),
                ConditionText = "\u2014"
            };
        }

        var iconFile = WeatherIconMapper.MapToIconFile(slot.Forecast.ConditionText, slot.Forecast.ForecastTime);
        var iconPath = WeatherIconPaths.Resolve(iconFile);
        var detailParts = new List<string>();
        if (slot.Forecast.PrecipitationProbability > 0)
        {
            detailParts.Add($"{slot.Forecast.PrecipitationProbability}%");
        }

        if (slot.Forecast.PrecipitationMm > 0)
        {
            detailParts.Add($"{slot.Forecast.PrecipitationMm:0.#}mm");
        }

        return new HourlyForecastItemViewModel
        {
            HasData = true,
            IconPath = File.Exists(iconPath) ? iconPath : WeatherIconPaths.Resolve("partly-cloudy-day.png"),
            TimeLabel = HourlyForecastTimeline.FormatHourOnlyLabel(slot.Hour),
            ConditionText = string.IsNullOrWhiteSpace(slot.Forecast.ConditionText) ? "\u2014" : slot.Forecast.ConditionText,
            DetailText = detailParts.Count == 0 ? null : string.Join(" \u00b7 ", detailParts)
        };
    }
}
