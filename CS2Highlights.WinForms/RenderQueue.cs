using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.Database.Entities;
using CS2Highlights.Renderer;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.WinForms;

/// <summary>
/// Singleton service. Accepts RenderJobs, saves them to DB, and processes them sequentially
/// — one CS2/HLAE launch per demo batch. Reports state changes via IProgress&lt;RenderProgress&gt;.
/// </summary>
public class RenderQueue
{
    private readonly CfgScriptBuilder _cfgBuilder;
    private readonly HlaeRenderer _hlaeRenderer;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SettingsRepository _settings;

    private readonly Queue<RenderJob> _pending = new();
    private readonly object _pendingLock = new();
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private CancellationTokenSource _cts = new();

    /// <summary>Set by RenderPanel on the UI thread so Progress&lt;T&gt; marshals callbacks correctly.</summary>
    public IProgress<RenderProgress>? Progress { get; set; }

    public RenderQueue(
        CfgScriptBuilder cfgBuilder,
        HlaeRenderer hlaeRenderer,
        IDbContextFactory<AppDbContext> dbFactory,
        SettingsRepository settings)
    {
        _cfgBuilder   = cfgBuilder;
        _hlaeRenderer = hlaeRenderer;
        _dbFactory    = dbFactory;
        _settings     = settings;
    }

    public void Enqueue(IReadOnlyList<RenderJob> jobs)
    {
        if (jobs.Count == 0) return;

        using var db  = _dbFactory.CreateDbContext();
        var now       = DateTime.UtcNow;

        foreach (var job in jobs)
        {
            var entity = new RenderJobEntity
            {
                HighlightId = job.HighlightId,
                QueuedAt    = now,
                Status      = RenderStatus.Queued
            };
            db.RenderJobs.Add(entity);
            db.SaveChanges();
            job.JobId = entity.Id;

            lock (_pendingLock) _pending.Enqueue(job);

            Progress?.Report(new RenderProgress
            {
                JobId       = job.JobId,
                HighlightId = job.HighlightId,
                Status      = RenderStatus.Queued
            });
        }

        _ = TryProcessAsync();
    }

    public void Cancel()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    private async Task TryProcessAsync()
    {
        if (!await _processLock.WaitAsync(0)) return;
        try
        {
            while (true)
            {
                List<RenderJob> batch;
                lock (_pendingLock)
                {
                    if (_pending.Count == 0) return;
                    batch = [.. _pending];
                    _pending.Clear();
                }
                await ProcessBatchAsync(batch, _cts.Token);
            }
        }
        finally
        {
            _processLock.Release();
            // If new jobs arrived during the last ProcessBatch, kick off again.
            lock (_pendingLock)
            {
                if (_pending.Count > 0) _ = TryProcessAsync();
            }
        }
    }

    private async Task ProcessBatchAsync(List<RenderJob> jobs, CancellationToken ct)
    {
        var hlaeExe     = _settings.Get(SettingsKeys.HlaeExePath)  ?? string.Empty;
        var cfgFolder   = _settings.Get(SettingsKeys.CfgFolder)    ?? string.Empty;
        var clipsFolder = _settings.Get(SettingsKeys.ClipsFolder)  ?? string.Empty;

        if (string.IsNullOrEmpty(hlaeExe))
        {
            FailAll(jobs, "HLAE path is not configured — go to Settings.");
            return;
        }

        foreach (var job in jobs)
        {
            UpdateDb(job.JobId, RenderStatus.Rendering, startedAt: DateTime.UtcNow);
            Progress?.Report(new RenderProgress
            {
                JobId       = job.JobId,
                HighlightId = job.HighlightId,
                Status      = RenderStatus.Rendering
            });
        }

        IReadOnlyList<BatchResult> batchResults;
        try
        {
            batchResults = _cfgBuilder.Build(jobs, cfgFolder, clipsFolder);
        }
        catch (Exception ex)
        {
            FailAll(jobs, $"CFG build failed: {ex.Message}");
            return;
        }

        foreach (var result in batchResults)
        {
            if (ct.IsCancellationRequested)
            {
                var ids = result.Clips.Select(c => c.HighlightId).ToHashSet();
                FailAll(jobs.Where(j => ids.Contains(j.HighlightId)).ToList(), "Cancelled.");
                continue;
            }

            var logProgress = new Progress<string>(
                msg => Progress?.Report(new RenderProgress { LogMessage = msg }));

            var renderResult = await _hlaeRenderer.RenderAsync(result, hlaeExe, logProgress, ct);

            foreach (var clip in result.Clips)
            {
                var job = jobs.FirstOrDefault(j => j.HighlightId == clip.HighlightId);
                if (job == null) continue;

                if (renderResult.Success)
                {
                    UpdateDb(job.JobId, RenderStatus.Done, finishedAt: DateTime.UtcNow, clipPath: clip.ClipPath);
                    Progress?.Report(new RenderProgress
                    {
                        JobId       = job.JobId,
                        HighlightId = job.HighlightId,
                        Status      = RenderStatus.Done,
                        ClipPath    = clip.ClipPath
                    });
                }
                else
                {
                    UpdateDb(job.JobId, RenderStatus.Failed, finishedAt: DateTime.UtcNow, error: renderResult.ErrorMessage);
                    Progress?.Report(new RenderProgress
                    {
                        JobId       = job.JobId,
                        HighlightId = job.HighlightId,
                        Status      = RenderStatus.Failed,
                        LogMessage  = renderResult.ErrorMessage
                    });
                }
            }
        }
    }

    private void FailAll(IEnumerable<RenderJob> jobs, string error)
    {
        foreach (var job in jobs)
        {
            UpdateDb(job.JobId, RenderStatus.Failed, finishedAt: DateTime.UtcNow, error: error);
            Progress?.Report(new RenderProgress
            {
                JobId       = job.JobId,
                HighlightId = job.HighlightId,
                Status      = RenderStatus.Failed,
                LogMessage  = error
            });
        }
    }

    private void UpdateDb(int dbJobId, RenderStatus status,
        DateTime? startedAt = null, DateTime? finishedAt = null,
        string? clipPath = null, string? error = null)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = db.RenderJobs.Find(dbJobId);
            if (entity == null) return;
            entity.Status = status;
            if (startedAt  != null) entity.StartedAt    = startedAt;
            if (finishedAt != null) entity.FinishedAt   = finishedAt;
            if (clipPath   != null) entity.ClipPath     = clipPath;
            if (error      != null) entity.ErrorMessage = error;
            db.SaveChanges();
        }
        catch { /* DB failure must not crash the render */ }
    }
}
