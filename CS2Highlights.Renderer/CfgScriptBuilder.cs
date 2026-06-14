using System.Text;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Renderer;

public record ClipOutput(int HighlightId, string ClipPath);
public record BatchResult(string CfgPath, IReadOnlyList<ClipOutput> Clips);

public class CfgScriptBuilder
{
    private const int TickRate = 64;
    private const long SteamId64Base = 76561197960265728L;

    // Groups jobs by demo path and writes one .cfg per demo.
    // CS2 launches once per demo and records all clips in a single session.
    public IReadOnlyList<BatchResult> Build(
        IReadOnlyList<RenderJob> jobs, string cfgFolder, string clipsFolder)
    {
        if (jobs.Count == 0) return [];

        return jobs
            .GroupBy(j => j.DemoPath, StringComparer.OrdinalIgnoreCase)
            .Select(g => BuildForDemo(g.OrderBy(j => j.TickStart).ToList(), cfgFolder, clipsFolder))
            .ToList();
    }

    private BatchResult BuildForDemo(
        List<RenderJob> jobs, string cfgFolder, string clipsFolder)
    {
        var settings   = jobs[0].Settings;
        var demoPath   = jobs[0].DemoPath;
        var ffmpegArgs = BuildFfmpegArgs(settings);

        var entries = jobs.Select(j => (
            job:       j,
            startTick: Math.Max(0, j.TickStart - j.Settings.BufferBeforeSeconds * TickRate),
            stopTick:  j.TickEnd + j.Settings.BufferAfterSeconds * TickRate,
            clipDir:   Path.Combine(clipsFolder, $"clip_{j.HighlightId}")
        )).ToList();

        var seekTick = Math.Max(0, entries[0].startTick - TickRate);

        var sb = new StringBuilder();
        sb.AppendLine("// cs2highlights auto-generated");
        sb.AppendLine("mirv_streams record screen enabled 1");
        sb.AppendLine($"mirv_streams record fps {settings.OutputFps}");
        sb.AppendLine();
        sb.AppendLine($"mirv_streams settings add ffmpeg cs2hl_enc \"{ffmpegArgs}\"");
        sb.AppendLine("mirv_streams settings edit afxDefault settings cs2hl_enc");
        sb.AppendLine();

        var clipOutputs = new List<ClipOutput>();
        for (int i = 0; i < entries.Count; i++)
        {
            var (job, startTick, stopTick, clipDir) = entries[i];
            var isLast    = i == entries.Count - 1;
            var accountId = ToAccountId(job.PlayerSteamId);
            var dirFwd    = clipDir.Replace('\\', '/'); // forward slashes safe inside nested quoted command

            var stopCmd = isLast ? "mirv_streams record end; quit" : "mirv_streams record end";

            sb.AppendLine($"mirv_cmd addAtTick {startTick} \"mirv_streams record name \\\"{dirFwd}\\\"; spec_lock_to_accountid {accountId}; mirv_streams record start\"");
            sb.AppendLine($"mirv_cmd addAtTick {stopTick} \"{stopCmd}\"");

            clipOutputs.Add(new ClipOutput(job.HighlightId, Path.Combine(clipDir, "clip.mp4")));
        }

        sb.AppendLine();
        sb.AppendLine($"playdemo \"{demoPath}\"");
        sb.AppendLine($"demo_gototick {seekTick}");

        Directory.CreateDirectory(cfgFolder);
        var demoName = Path.GetFileNameWithoutExtension(demoPath);
        var cfgPath  = Path.Combine(cfgFolder, $"{demoName}_{jobs[0].HighlightId}.cfg");
        File.WriteAllText(cfgPath, sb.ToString());

        return new BatchResult(cfgPath, clipOutputs);
    }

    private static string BuildFfmpegArgs(RenderSettings settings)
    {
        var scale       = settings.OutputResolution.Replace("x", ":");
        var encoderArgs = settings.Encoder switch
        {
            "h264_nvenc" => "-c:v h264_nvenc -preset p4 -b:v 20M -pix_fmt yuv420p",
            "libx264"    => "-c:v libx264 -crf 18 -preset medium -pix_fmt yuv420p",
            _            => $"-c:v {settings.Encoder} -pix_fmt yuv420p"
        };
        // {QUOTE} and {AFX_STREAM_PATH} are HLAE template variables, literal in the .cfg
        return $"{encoderArgs} -vf scale={scale} {{QUOTE}}{{AFX_STREAM_PATH}}\\\\clip.mp4{{QUOTE}}";
    }

    private static long ToAccountId(string steamId64)
    {
        if (long.TryParse(steamId64, out var id))
            return id - SteamId64Base;
        return 0;
    }
}
