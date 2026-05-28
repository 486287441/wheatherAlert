using System.Windows.Forms;
using WeatherAlert.Core.Abstractions;

namespace WeatherAlert.App.Tray;

public sealed class HistoryWindow : Form
{
    private readonly INotificationHistoryRepository _historyRepository;
    private readonly DataGridView _grid;

    public HistoryWindow(INotificationHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
        Text = "天气提醒历史";
        Width = 820;
        Height = 420;
        StartPosition = FormStartPosition.CenterScreen;

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        Controls.Add(_grid);
        Shown += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var rows = await _historyRepository.GetRecentAsync(100, CancellationToken.None);
        _grid.DataSource = rows.Select(x => new
        {
            CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            Type = x.Type.ToString(),
            x.CityCode,
            x.Title,
            x.Body
        }).ToList();
    }
}
