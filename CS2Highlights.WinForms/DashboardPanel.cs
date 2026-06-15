using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.DemoScanner;
using CS2Highlights.Parser;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.WinForms;

public class DashboardPanel : UserControl
{
    private readonly DemoFolderScanner _scanner;
    private readonly LightweightDemoReader _reader;
    private readonly DemoParser _parser;
    private readonly HighlightService _highlightService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SettingsRepository _settings;

    private DataGridView _grid = null!;
    private Button _parseButton = null!;
    private Button _detailButton = null!;
    private Button _scanButton = null!;
    private Label _folderLabel = null!;
    private Label _statusLabel = null!;
    private readonly ToolTip _folderTooltip = new();

    public event EventHandler<ParsedMatch>? MatchParsed;

    public DashboardPanel(
        DemoFolderScanner scanner,
        LightweightDemoReader reader,
        DemoParser parser,
        HighlightService highlightService,
        IDbContextFactory<AppDbContext> dbFactory,
        SettingsRepository settings)
    {
        _scanner = scanner;
        _reader = reader;
        _parser = parser;
        _highlightService = highlightService;
        _dbFactory = dbFactory;
        _settings = settings;
        BuildLayout();
        LoadDemos();
    }

    private void BuildLayout()
    {
        Padding = new Padding(8);

        // Top bar: caption | folder path (truncated, tooltip) | Scan button
        var topTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };
        topTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // "Demos folder:"
        topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // path — fills remaining
        topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));   // Scan button

        var caption = new Label
        {
            Text = "Demos folder:",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 0)
        };

        _folderLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText
        };

        _scanButton = new Button
        {
            Text = "Scan",
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 4, 0, 4)
        };
        _scanButton.Click += (_, _) => LoadDemos();

        topTable.Controls.Add(caption,      0, 0);
        topTable.Controls.Add(_folderLabel, 1, 0);
        topTable.Controls.Add(_scanButton,  2, 0);

        var topBar = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(0, 6, 0, 0) };
        topBar.Controls.Add(topTable);

        // Main grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "File",    FillWeight = 32 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Map",      HeaderText = "Map",     FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size",     HeaderText = "Size",    FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",     HeaderText = "Date",    FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Parsed",   HeaderText = "Parsed?", FillWeight = 14 });
        _grid.SelectionChanged += (_, _) =>
        {
            var hasRow = _grid.SelectedRows.Count > 0;
            _parseButton.Enabled  = hasRow;
            _detailButton.Enabled = hasRow;
        };

        // Bottom bar: Parse button | Detail button | status
        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        _parseButton  = new Button { Text = "Parse Selected Demo", Location = new Point(0, 7),   Width = 155, Height = 26, Enabled = false };
        _detailButton = new Button { Text = "Detail",              Location = new Point(161, 7), Width = 70,  Height = 26, Enabled = false };
        _parseButton.Click  += ParseButton_Click;
        _detailButton.Click += DetailButton_Click;
        _statusLabel = new Label { Location = new Point(238, 11), AutoSize = true, ForeColor = SystemColors.GrayText };
        bottomBar.Controls.AddRange([_parseButton, _detailButton, _statusLabel]);

        Controls.Add(_grid);
        Controls.Add(bottomBar);
        Controls.Add(topBar);
    }

    public void LoadDemos()
    {
        var folder = _settings.Get(SettingsKeys.DemosFolder) ?? string.Empty;
        _folderLabel.Text = string.IsNullOrEmpty(folder) ? "(not configured — go to Settings)" : folder;
        _folderTooltip.SetToolTip(_folderLabel, folder);

        _grid.Rows.Clear();
        if (string.IsNullOrEmpty(folder)) return;

        var demos = _scanner.ScanFolder(folder);

        using var db = _dbFactory.CreateDbContext();
        var parsedPaths = db.Matches.Select(m => m.DemoPath).ToHashSet();
        var cachedMaps  = db.DemoDetails
            .Select(d => new { d.FileName, d.MapName })
            .ToDictionary(d => d.FileName, d => d.MapName);

        foreach (var demo in demos)
        {
            var mapName = cachedMaps.TryGetValue(demo.FileName, out var m) ? m : "—";
            _grid.Rows.Add(
                demo.FileName,
                mapName,
                FormatSize(demo.FileSizeBytes),
                demo.LastModified.ToString("yyyy-MM-dd HH:mm"),
                parsedPaths.Contains(demo.FilePath) ? "Yes" : "—");
            _grid.Rows[^1].Tag = demo;
        }
    }

    private async void ParseButton_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;
        var demo = (DemoFileInfo)_grid.SelectedRows[0].Tag!;

        _parseButton.Enabled = false;
        _scanButton.Enabled = false;
        SetStatus("Reading players…", SystemColors.GrayText);

        try
        {
            var header = await _reader.ReadHeaderAsync(demo.FilePath);

            using var picker = new PlayerPickerDialog(header);
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedPlayer == null)
                return;

            SetStatus("Parsing demo…", SystemColors.GrayText);
            var match = await _parser.ParseAsync(demo.FilePath, picker.SelectedPlayer);

            SetStatus("Detecting highlights…", SystemColors.GrayText);
            var highlights = await _highlightService.RunAsync(match, new DetectionOptions());

            SetStatus($"Done — {match.Rounds.Count} rounds, {highlights.Count} highlight(s) found.", Color.ForestGreen);
            MatchParsed?.Invoke(this, match);
            LoadDemos();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", Color.Firebrick);
            MessageBox.Show(ex.Message, "Parse failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _parseButton.Enabled = true;
            _scanButton.Enabled = true;
        }
    }

    private void DetailButton_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;
        var demo = (DemoFileInfo)_grid.SelectedRows[0].Tag!;
        var row  = _grid.SelectedRows[0];

        using var form = new DemoOverviewForm(demo, _dbFactory, _reader);
        form.ShowDialog(this);

        // After the form closes, refresh Map column in case it was just cached for the first time.
        using var db = _dbFactory.CreateDbContext();
        var cached = db.DemoDetails.FirstOrDefault(d => d.FileName == demo.FileName);
        if (cached != null)
            row.Cells["Map"].Value = cached.MapName;
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:0.#} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:0.#} MB",
        >= 1_024         => $"{bytes / 1_024.0:0.#} KB",
        _                => $"{bytes} B"
    };
}
