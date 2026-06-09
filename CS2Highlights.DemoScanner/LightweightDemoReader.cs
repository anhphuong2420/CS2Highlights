using System.Security.Cryptography;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;
using DemoFile;

namespace CS2Highlights.DemoScanner;

public class LightweightDemoReader : ILightweightDemoReader
{
    public async Task<DemoHeaderInfo> ReadHeaderAsync(string demoPath, CancellationToken cancellationToken = default)
    {
        var info = new DemoHeaderInfo
        {
            FilePath = demoPath,
            MatchDate = File.GetLastWriteTime(demoPath),
            MatchId = ComputeMatchId(demoPath)
        };

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var stream = File.OpenRead(demoPath);
        var demo = new CsDemoParser();

        // Collect every player entity ever created — catches players who disconnect mid-match
        var seenPlayers = new Dictionary<ulong, PlayerInfo>();

        demo.EntityEvents.CCSPlayerController.Create += controller =>
        {
            if (controller.SteamID > 0)
                seenPlayers[controller.SteamID] = new PlayerInfo
                {
                    SteamId = controller.SteamID.ToString(),
                    PlayerName = controller.PlayerName ?? string.Empty,
                    Team = controller.CSTeamNum.ToString()
                };
        };

        // Stop after signon + one game tick: all 10 players have been created by then
        demo.OnCommandFinish += () =>
        {
            if (demo.FileHeader is { } header)
                info.MapName = header.MapName ?? string.Empty;

            if (demo.CurrentGameTick.Value > 0 && seenPlayers.Count >= 10)
                linkedCts.Cancel();
        };

        var reader = DemoFileReader.Create(demo, stream);
        try
        {
            await reader.ReadAllAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }

        info.Players = seenPlayers.Values.ToList();

        return info;
    }

    // SHA-256 of first 64 KB — stable across renames, fast
    private static string ComputeMatchId(string demoPath)
    {
        using var stream = File.OpenRead(demoPath);
        var buffer = new byte[65536];
        var read = stream.Read(buffer, 0, buffer.Length);
        var hash = SHA256.HashData(buffer.AsSpan(0, read));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
