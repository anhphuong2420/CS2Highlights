using CS2Highlights.Database;

namespace CS2Highlights.WinForms;

public class SettingsPanel : UserControl
{
    private readonly SettingsRepository _settings;

    private TextBox _hlaePathBox    = null!;
    private TextBox _ffmpegPathBox  = null!;
    private TextBox _demosFolderBox = null!;
    private TextBox _clipsFolderBox = null!;
    private TextBox _cfgFolderBox   = null!;
    private Label   _statusLabel    = null!;

    public SettingsPanel(SettingsRepository settings)
    {
        _settings = settings;
        BuildLayout();
        LoadSettings();
    }

    private void BuildLayout()
    {
        Padding = new Padding(16);

        var group = new GroupBox
        {
            Text = "Paths",
            Dock = DockStyle.Top,
            Padding = new Padding(12),
            Height = 270
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 5
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        for (var i = 0; i < 5; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        _hlaePathBox    = AddRow(table, 0, "HLAE Executable:",    false);
        _ffmpegPathBox  = AddRow(table, 1, "FFmpeg Executable:",  false);
        _demosFolderBox = AddRow(table, 2, "Demos Folder:",       true);
        _clipsFolderBox = AddRow(table, 3, "Clips Folder:",       true);
        _cfgFolderBox   = AddRow(table, 4, "CFG Folder:",         true);

        group.Controls.Add(table);

        var saveBtn = new Button { Text = "Save Settings", Width = 130, Height = 32 };
        saveBtn.Click += (_, _) => SaveSettings();

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.ForestGreen,
            TextAlign = ContentAlignment.MiddleLeft,
            Visible = false
        };

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(0, 6, 0, 0)
        };
        bottom.Controls.Add(saveBtn);

        Controls.Add(_statusLabel);
        Controls.Add(bottom);
        Controls.Add(group);
    }

    private TextBox AddRow(TableLayoutPanel table, int row, string labelText, bool isFolder)
    {
        var label = new Label
        {
            Text = labelText,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var box = new TextBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(4, 10, 4, 4)
        };

        var browse = new Button
        {
            Text = "Browse…",
            Anchor = AnchorStyles.Right,
            Margin = new Padding(4, 8, 0, 4)
        };
        browse.Click += (_, _) => BrowsePath(box, isFolder);

        table.Controls.Add(label,  0, row);
        table.Controls.Add(box,    1, row);
        table.Controls.Add(browse, 2, row);

        return box;
    }

    private void LoadSettings()
    {
        _hlaePathBox.Text    = _settings.Get(SettingsKeys.HlaeExePath)   ?? string.Empty;
        _ffmpegPathBox.Text  = _settings.Get(SettingsKeys.FfmpegExePath) ?? string.Empty;
        _demosFolderBox.Text = _settings.Get(SettingsKeys.DemosFolder)   ?? string.Empty;
        _clipsFolderBox.Text = _settings.Get(SettingsKeys.ClipsFolder)   ?? string.Empty;
        _cfgFolderBox.Text   = _settings.Get(SettingsKeys.CfgFolder)     ?? string.Empty;
    }

    private void SaveSettings()
    {
        _settings.Set(SettingsKeys.HlaeExePath,  _hlaePathBox.Text.Trim());
        _settings.Set(SettingsKeys.FfmpegExePath, _ffmpegPathBox.Text.Trim());
        _settings.Set(SettingsKeys.DemosFolder,  _demosFolderBox.Text.Trim());
        _settings.Set(SettingsKeys.ClipsFolder,  _clipsFolderBox.Text.Trim());
        _settings.Set(SettingsKeys.CfgFolder,    _cfgFolderBox.Text.Trim());

        _statusLabel.Text    = "Settings saved.";
        _statusLabel.Visible = true;

        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
        timer.Tick += (_, _) => { _statusLabel.Visible = false; timer.Stop(); timer.Dispose(); };
        timer.Start();
    }

    private static void BrowsePath(TextBox target, bool isFolder)
    {
        if (isFolder)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select folder",
                SelectedPath = target.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                target.Text = dlg.SelectedPath;
        }
        else
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                InitialDirectory = Path.GetDirectoryName(target.Text) ?? string.Empty,
                FileName = Path.GetFileName(target.Text)
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                target.Text = dlg.FileName;
        }
    }
}
