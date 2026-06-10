using CS2Highlights.Core.Models;

namespace CS2Highlights.WinForms;

public class PlayerPickerDialog : Form
{
    public PlayerInfo? SelectedPlayer { get; private set; }

    private readonly ListView _playerList;

    public PlayerPickerDialog(DemoHeaderInfo header)
    {
        Text = "Select Player";
        Size = new Size(560, 460);
        MinimumSize = new Size(480, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var infoLabel = new Label
        {
            Text = $"Map: {header.MapName}    Date: {header.MatchDate:yyyy-MM-dd HH:mm}",
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };

        _playerList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            HideSelection = false
        };
        _playerList.Columns.Add("Team",      100);
        _playerList.Columns.Add("Name",      220);
        _playerList.Columns.Add("Steam ID",  160);
        _playerList.DoubleClick += (_, _) => Confirm();

        foreach (var player in header.Players.OrderBy(p => p.Team).ThenBy(p => p.PlayerName))
        {
            var item = new ListViewItem(player.Team);
            item.SubItems.Add(player.PlayerName);
            item.SubItems.Add(player.SteamId);
            item.Tag = player;
            _playerList.Items.Add(item);
        }

        var selectBtn = new Button { Text = "Select Player", Width = 120, Height = 32, DialogResult = DialogResult.None };
        var cancelBtn = new Button { Text = "Cancel",        Width = 80,  Height = 32, DialogResult = DialogResult.Cancel };
        selectBtn.Click += (_, _) => Confirm();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(8, 6, 8, 0)
        };
        btnPanel.Controls.Add(selectBtn);
        btnPanel.Controls.Add(cancelBtn);

        Controls.Add(_playerList);
        Controls.Add(btnPanel);
        Controls.Add(infoLabel);

        AcceptButton = selectBtn;
        CancelButton = cancelBtn;
    }

    private void Confirm()
    {
        if (_playerList.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a player.", "No selection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SelectedPlayer = (PlayerInfo)_playerList.SelectedItems[0].Tag!;
        DialogResult = DialogResult.OK;
        Close();
    }
}
