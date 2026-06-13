using CS2Highlights.Database;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.WinForms;

public class MatchesPanel : UserControl
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private DataGridView _grid = null!;

    public MatchesPanel(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        BuildLayout();
        LoadMatches();
    }

    private void BuildLayout()
    {
        Padding = new Padding(8);

        var topBar = new Panel { Dock = DockStyle.Top, Height = 38 };
        var title = new Label
        {
            Text = "Parsed Matches",
            Location = new Point(0, 10),
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };
        var refreshBtn = new Button { Text = "Refresh", Location = new Point(420, 6), Width = 80, Height = 26 };
        refreshBtn.Click += (_, _) => LoadMatches();
        topBar.Controls.AddRange([title, refreshBtn]);

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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "File",       HeaderText = "File",       FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Map",        HeaderText = "Map",        FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",       HeaderText = "Date",       FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Player",     HeaderText = "Player",     FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Highlights", HeaderText = "Highlights", FillWeight = 12 });
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenDetails(); };

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        var detailsBtn = new Button { Text = "View Details", Location = new Point(0, 7), Width = 110, Height = 26 };
        detailsBtn.Click += (_, _) => OpenDetails();
        bottomBar.Controls.Add(detailsBtn);

        Controls.Add(_grid);
        Controls.Add(bottomBar);
        Controls.Add(topBar);
    }

    public void LoadMatches()
    {
        _grid.Rows.Clear();
        using var db = _dbFactory.CreateDbContext();

        var matches = db.Matches.OrderByDescending(m => m.Date).ToList();
        foreach (var m in matches)
        {
            var count = db.Highlights.Count(h => h.MatchId == m.Id);
            _grid.Rows.Add(m.DemoFileName, m.Map, m.Date.ToString("yyyy-MM-dd"), m.SelectedPlayerName, count);
            _grid.Rows[^1].Tag = m.Id;
        }
    }

    private void OpenDetails()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var matchId = (int)_grid.SelectedRows[0].Tag!;
        new MatchDetailForm(matchId, _dbFactory).Show(this);
    }
}
