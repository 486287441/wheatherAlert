using System.Windows.Forms;

namespace WeatherAlert.App.Tray;

public sealed class CitySelectionDialog : Form
{
    private readonly ComboBox _cityCombo;

    public CitySelectionDialog(IReadOnlyDictionary<string, string> cityMap, string currentCityCode)
    {
        Text = "切换城市";
        Width = 360;
        Height = 140;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _cityCombo = new ComboBox
        {
            Left = 16,
            Top = 16,
            Width = 310,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        foreach (var item in cityMap)
        {
            _cityCombo.Items.Add(new CityItem(item.Key, item.Value));
        }

        _cityCombo.SelectedItem = _cityCombo.Items
            .Cast<CityItem>()
            .FirstOrDefault(x => x.Code == currentCityCode) ?? _cityCombo.Items.Cast<CityItem>().FirstOrDefault();

        var okButton = new Button
        {
            Text = "确定",
            Left = 170,
            Top = 56,
            Width = 75,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "取消",
            Left = 251,
            Top = 56,
            Width = 75,
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(_cityCombo);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public string? SelectedCityCode => (_cityCombo.SelectedItem as CityItem)?.Code;

    private sealed record CityItem(string Code, string Name)
    {
        public override string ToString() => $"{Name} ({Code})";
    }
}
