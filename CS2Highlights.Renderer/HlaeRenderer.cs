using System.Diagnostics;

namespace CS2Highlights.Renderer;

public record HlaeRenderResult(bool Success, string? ErrorMessage, IReadOnlyList<ClipOutput> Clips);

public class HlaeRenderer
{
    /// <summary>
    /// Launches HLAE with the .cfg from <paramref name="batch"/>, waits for CS2 to finish,
    /// then verifies all expected output files exist.
    /// </summary>
    public async Task<HlaeRenderResult> RenderAsync(
        BatchResult batch,
        string hlaeExePath,
        IProgress<string>? log = null,
        CancellationToken ct = default)
    {
        var args = BuildArgs(batch.CfgPath);
        log?.Report($"[hlae] {hlaeExePath} {args}");

        var psi = new ProcessStartInfo
        {
            FileName               = hlaeExePath,
            Arguments              = args,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };

        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) log?.Report($"[hlae] {e.Data}"); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data != null) log?.Report($"[hlae:err] {e.Data}"); };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new HlaeRenderResult(false, $"Failed to start HLAE: {ex.Message}", []);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        log?.Report($"[hlae] Process started (PID {process.Id}). Waiting for CS2 to finish…");

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            log?.Report("[hlae] Cancelled — CS2 killed.");
            return new HlaeRenderResult(false, "Cancelled by user.", []);
        }

        log?.Report($"[hlae] Process exited (code {process.ExitCode}).");

        if (process.ExitCode != 0)
            return new HlaeRenderResult(false, $"HLAE exited with code {process.ExitCode}.", []);

        var missing = batch.Clips.Where(c => !File.Exists(c.ClipPath)).ToList();
        if (missing.Count > 0)
        {
            var names = string.Join(", ", missing.Select(c => Path.GetFileName(c.ClipPath)));
            return new HlaeRenderResult(false, $"Output file(s) not found after render: {names}", []);
        }

        log?.Report($"[hlae] Done — {batch.Clips.Count} clip(s) ready.");
        return new HlaeRenderResult(true, null, batch.Clips);
    }

    private static string BuildArgs(string cfgPath) =>
        $"-csgo -steam -autoConfig \"{cfgPath}\" -noGui";
}
