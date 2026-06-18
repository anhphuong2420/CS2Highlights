using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.WinForms;

public class RenderPanel : UserControl
{
    private readonly RenderQueue _queue;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private DataGridView _grid = null!;
    private ListBox _logBox = null!;
    private Button _cancelBtn = null!;
    private Label _statusLabel = null!;

    private readonly Dictionary<int, int> _jobRowIndex = []; // dbJobId → row index

    public RenderPanel(RenderQueue queue, IDbContextFactory<AppDbContext> dbFactory)
    {
        _queue    = queue;
        _dbFactory = dbFactory;
        BuildLayout();
        // Set up progress after the message loop starts (ensures WindowsFormsSynchronizationContext is installed).
        Load += (_, _) => _queue.Progress = new Progress<RenderProgress>(OnProgress);
    }

    private void BuildLayout()
    {
        Padding = new Padding(8);

        var topBar = new Panel { Dock = DockStyle.Top, Height = 38 };
        var title  = new Label
        {
            Text = "Render Queue", Location = new Point(0, 10), AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };
        _cancelBtn = new Button
        {
            Text = "Cancel", Location = new Point(120, 6), Width = 80, Height = 26, Enabled = false
        };
        _cancelBtn.Click += (_, _) => _queue.Cancel();
        _statusLabel = new Label
        {
            Location = new Point(210, 11), AutoSize = true, ForeColor = SystemColors.GrayText,
            Text = "No jobs."
        };
        topBar.Controls.AddRange([title, _cancelBtn, _statusLabel]);

        var split = new SplitContainer
        {
            Dock              = DockStyle.Fill,
            Orientation       = Orientation.Horizontal,
            SplitterDistance  = 260,
            Panel1MinSize     = 80,
            Panel2MinSize     = 60
        };

        _grid = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible     = false,
            BackgroundColor       = SystemColors.Window
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",   HeaderText = "Type",        FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desc",   HeaderText = "Description", FillWeight = 44 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status",      FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Clip",   HeaderText = "Clip",        FillWeight = 22 });
        split.Panel1.Controls.Add(_grid);

        var logLabel = new Label
        {
            Text = "HLAE Log", Dock = DockStyle.Top, Height = 20,
            ForeColor = SystemColors.GrayText, Padding = new Padding(4, 2, 0, 0)
        };
        _logBox = new ListBox
        {
            Dock                = DockStyle.Fill,
            Font                = new Font("Consolas", 8f),
            HorizontalScrollbar = true
        };
        split.Panel2.Controls.Add(_logBox);
        split.Panel2.Controls.Add(logLabel);

        Controls.Add(split);
        Controls.Add(topBar);
    }

    private void OnProgress(RenderProgress p)
    {
        if (p.LogMessage != null)
        {
            _logBox.Items.Add(p.LogMessage);
            if (_logBox.Items.Count > 1000) _logBox.Items.RemoveAt(0);
            _logBox.TopIndex = _logBox.Items.Count - 1;
        }

        if (p.JobId == 0) return; // log-only, no state change

        switch (p.Status)
        {
            case RenderStatus.Queued:
                AddJobRow(p.JobId, p.HighlightId);
                break;

            case RenderStatus.Rendering:
                SetRowStatus(p.JobId, "Rendering…", Color.FromArgb(255, 248, 200));
                _cancelBtn.Enabled = true;
                break;

            case RenderStatus.Done:
                SetRowStatus(p.JobId, "Done ✓", Color.FromArgb(198, 239, 206), p.ClipPath);
                _cancelBtn.Enabled = false;
                break;

            case RenderStatus.Failed:
                SetRowStatus(p.JobId, "Failed ✗", Color.FromArgb(255, 199, 206));
                _cancelBtn.Enabled = false;
                break;
        }

        RefreshStatusLabel();
    }

    private void AddJobRow(int dbJobId, int highlightId)
    {
        using var db = _dbFactory.CreateDbContext();
        var h = db.Highlights.Find(highlightId);
        if (h == null) return;

        var type = h.HighlightType.HasValue ? $"✦ {h.HighlightType}" : $"✧ {h.LowlightType}";
        _grid.Rows.Add(type, h.Description, "Queued", "—");
        var idx = _grid.Rows.Count - 1;
        _grid.Rows[idx].Tag   = dbJobId;
        _jobRowIndex[dbJobId] = idx;
    }

    private void SetRowStatus(int dbJobId, string status, Color bg, string? clipPath = null)
    {
        if (!_jobRowIndex.TryGetValue(dbJobId, out var idx)) return;
        var row = _grid.Rows[idx];
        row.Cells["Status"].Value      = status;
        row.DefaultCellStyle.BackColor = bg;
        if (clipPath != null)
            row.Cells["Clip"].Value = Path.GetFileName(clipPath);
    }

    private void RefreshStatusLabel()
    {
        var rows      = _grid.Rows.Cast<DataGridViewRow>().ToList();
        var total     = rows.Count;
        var done      = rows.Count(r => (r.Cells["Status"].Value as string ?? "").StartsWith("Done"));
        var failed    = rows.Count(r => (r.Cells["Status"].Value as string ?? "").StartsWith("Failed"));
        var rendering = rows.Any(r => (r.Cells["Status"].Value as string ?? "") == "Rendering…");

        _statusLabel.Text = total == 0
            ? "No jobs."
            : rendering
            ? $"Rendering… ({done + failed}/{total} done)"
            : $"{done} done, {failed} failed, {total - done - failed} queued";
    }
}
