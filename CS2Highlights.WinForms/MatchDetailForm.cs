using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.WinForms;

public class MatchDetailForm : Form
{
    private readonly RenderQueue _queue;

    public MatchDetailForm(int matchEntityId, IDbContextFactory<AppDbContext> dbFactory, RenderQueue queue)
    {
        _queue = queue;

        using var db = dbFactory.CreateDbContext();

        var match = db.Matches.FirstOrDefault(m => m.Id == matchEntityId);
        if (match == null) { Close(); return; }

        var highlights = db.Highlights
            .Where(h => h.MatchId == matchEntityId)
            .OrderBy(h => h.TickStart)
            .ToList();

        var roundLookup = db.Rounds
            .Where(r => r.MatchId == matchEntityId)
            .ToDictionary(r => r.Id, r => r.RoundNumber);

        BuildLayout(match, highlights, roundLookup);
    }

    private void BuildLayout(MatchEntity match, List<HighlightEntity> highlights, Dictionary<int, int> roundLookup)
    {
        Text          = $"{match.Map} — {match.SelectedPlayerName}";
        Size          = new Size(740, 520);
        MinimumSize   = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;

        var header = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 0, 8, 0) };
        header.Controls.Add(new Label
        {
            Text      = $"Map: {match.Map}     Date: {match.Date:yyyy-MM-dd}     Player: {match.SelectedPlayerName}     File: {match.DemoFileName}",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        });

        var grid = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = true,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible     = false,
            BackgroundColor       = SystemColors.Window
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kind",  HeaderText = "Type",        FillWeight = 22 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Round", HeaderText = "Round",       FillWeight = 10 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desc",  HeaderText = "Description", FillWeight = 48 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ticks", HeaderText = "Ticks",       FillWeight = 20 });

        foreach (var h in highlights)
        {
            var roundNum = h.RoundId.HasValue && roundLookup.TryGetValue(h.RoundId.Value, out var rn) ? rn : 0;
            var kind     = h.HighlightType.HasValue ? $"✦ {h.HighlightType}" : $"✧ {h.LowlightType}";
            grid.Rows.Add(kind, $"R{roundNum}", h.Description, $"{h.TickStart}–{h.TickEnd}");
            grid.Rows[^1].Tag = h;
        }

        if (highlights.Count == 0)
            grid.Rows.Add("—", "—", "No highlights detected for this match.", "—");

        var bottom = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height        = 44,
            Padding       = new Padding(8, 6, 8, 0)
        };

        var closeBtn = new Button { Text = "Close", Width = 80, Height = 28 };
        closeBtn.Click += (_, _) => Close();

        var queueBtn = new Button { Text = "Add to Render Queue", Width = 155, Height = 28 };
        queueBtn.Click += (_, _) =>
        {
            // Queue selected rows, or all highlights if nothing is selected.
            var selected = grid.SelectedRows.Count > 0
                ? grid.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.Tag as HighlightEntity)
                    .Where(h => h != null)
                    .Cast<HighlightEntity>()
                    .ToList()
                : highlights;

            if (selected.Count == 0) return;

            var jobs = selected.Select(h => new RenderJob
            {
                HighlightId   = h.Id,
                DemoPath      = match.DemoPath,
                TickStart     = h.TickStart,
                TickEnd       = h.TickEnd,
                PlayerSteamId = match.SelectedPlayerSteamId,
                Settings      = new RenderSettings()
            }).ToList();

            _queue.Enqueue(jobs);

            MessageBox.Show(
                $"{jobs.Count} highlight(s) added to the render queue.",
                "Queued", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        bottom.Controls.Add(closeBtn);
        bottom.Controls.Add(queueBtn);

        Controls.Add(grid);
        Controls.Add(bottom);
        Controls.Add(header);
    }
}
