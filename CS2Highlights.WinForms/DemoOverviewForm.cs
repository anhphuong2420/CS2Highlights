using System.Text.Json;
using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.Database.Entities;
using CS2Highlights.DemoScanner;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.WinForms;

public class DemoOverviewForm : Form
{
    private readonly DemoFileInfo _demo;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LightweightDemoReader _reader;

    private Label _headerLabel = null!;
    private DataGridView _grid = null!;

    public DemoOverviewForm(
        DemoFileInfo demo,
        IDbContextFactory<AppDbContext> dbFactory,
        LightweightDemoReader reader)
    {
        _demo      = demo;
        _dbFactory = dbFactory;
        _reader    = reader;
        BuildLayout();
        Load += async (_, _) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        Text          = $"{_demo.FileName} — Overview";
        Size          = new Size(620, 460);
        MinimumSize   = new Size(480, 360);
        StartPosition = FormStartPosition.CenterParent;

        _headerLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 40,
            Padding   = new Padding(8, 10, 8, 0),
            Text      = "Loading…",
            Font      = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            ForeColor = SystemColors.GrayText
        };

        _grid = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible     = false,
            BackgroundColor       = SystemColors.Window
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Player", HeaderText = "Player", FillWeight = 44 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kills",  HeaderText = "K",      FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Deaths", HeaderText = "D",      FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "KD",     HeaderText = "K/D",    FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HS",     HeaderText = "HS%",    FillWeight = 14 });

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
        var closeBtn  = new Button { Text = "Close", Width = 80, Height = 28, Location = new Point(0, 8) };
        closeBtn.Click += (_, _) => Close();
        bottomBar.Controls.Add(closeBtn);

        Controls.Add(_grid);
        Controls.Add(bottomBar);
        Controls.Add(_headerLabel);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();

            var fileName = Path.GetFileName(_demo.FilePath);
            var cached   = db.DemoDetails.FirstOrDefault(d => d.FileName == fileName);

            List<PlayerInfo> players;
            string mapName;
            DateTime matchDate;

            if (cached != null)
            {
                mapName   = cached.MapName;
                matchDate = cached.MatchDate;
                players   = JsonSerializer.Deserialize<List<PlayerInfo>>(cached.PlayersJson) ?? [];
            }
            else
            {
                _headerLabel.Text      = "Reading demo header for the first time…";
                _headerLabel.ForeColor = SystemColors.GrayText;

                var header = await _reader.ReadHeaderAsync(_demo.FilePath);
                mapName   = header.MapName;
                matchDate = header.MatchDate;
                players   = header.Players;

                db.DemoDetails.Add(new DemoDetailEntity
                {
                    FileName    = fileName,
                    MapName     = mapName,
                    MatchDate   = matchDate,
                    PlayersJson = JsonSerializer.Serialize(players)
                });
                db.SaveChanges();
            }

            var match = db.Matches.FirstOrDefault(m => m.DemoPath == _demo.FilePath);

            _headerLabel.Text = match != null
                ? $"{mapName}   {matchDate:yyyy-MM-dd}   Selected: {match.SelectedPlayerName}"
                : $"{mapName}   {matchDate:yyyy-MM-dd}   (not yet parsed)";
            _headerLabel.ForeColor = SystemColors.ControlText;

            Dictionary<string, int> kills  = [];
            Dictionary<string, int> deaths = [];
            Dictionary<string, int> hs     = [];

            if (match != null)
            {
                var killEvents = db.KillEvents
                    .Where(k => k.MatchId == match.Id)
                    .Select(k => new { k.KillerSteamId, k.VictimSteamId, k.IsHeadshot })
                    .ToList();

                foreach (var e in killEvents)
                {
                    kills[e.KillerSteamId]  = kills.GetValueOrDefault(e.KillerSteamId)  + 1;
                    deaths[e.VictimSteamId] = deaths.GetValueOrDefault(e.VictimSteamId) + 1;
                    if (e.IsHeadshot)
                        hs[e.KillerSteamId] = hs.GetValueOrDefault(e.KillerSteamId) + 1;
                }
            }

            var selectedId = match?.SelectedPlayerSteamId ?? string.Empty;

            var rows = players
                .Select(p => new
                {
                    Label      = (p.SteamId == selectedId ? "★ " : "   ") + p.PlayerName,
                    K          = kills.GetValueOrDefault(p.SteamId),
                    D          = deaths.GetValueOrDefault(p.SteamId),
                    H          = hs.GetValueOrDefault(p.SteamId),
                    IsSelected = p.SteamId == selectedId
                })
                .OrderByDescending(r => r.K)
                .ThenBy(r => r.D)
                .ToList();

            _grid.Rows.Clear();
            foreach (var r in rows)
            {
                var kStr  = match == null ? "—" : r.K.ToString();
                var dStr  = match == null ? "—" : r.D.ToString();
                var kdStr = match == null ? "—"
                          : r.D == 0     ? $"{r.K}.00"
                          :                ((double)r.K / r.D).ToString("F2");
                var hsStr = match == null ? "—"
                          : r.K == 0     ? "0%"
                          :                $"{r.H * 100 / r.K}%";

                _grid.Rows.Add(r.Label, kStr, dStr, kdStr, hsStr);

                if (r.IsSelected)
                {
                    var row = _grid.Rows[^1];
                    row.DefaultCellStyle.Font      = new Font(_grid.Font, FontStyle.Bold);
                    row.DefaultCellStyle.BackColor = Color.FromArgb(225, 240, 255);
                }
            }
        }
        catch (Exception ex)
        {
            _headerLabel.Text      = $"Failed to load: {ex.Message}";
            _headerLabel.ForeColor = Color.Firebrick;
        }
    }
}
