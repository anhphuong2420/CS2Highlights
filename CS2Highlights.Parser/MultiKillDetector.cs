using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class MultiKillDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();

        if (!options.MultiKillEnabled)
            return Task.FromResult(highlights);

        var killsByRound = match.Kills
            .Where(k => k.SteamId == match.SelectedPlayer.SteamId)
            .GroupBy(k => k.RoundNumber);

        foreach (var group in killsByRound)
        {
            var roundKills = group.OrderBy(k => k.Tick).ToList();
            if (roundKills.Count < options.MultiKillMinKills) continue;

            var killCount = roundKills.Count;
            var type = killCount >= 5 ? HighlightType.MultiKill5
                     : killCount == 4 ? HighlightType.MultiKill4
                     : HighlightType.MultiKill3;

            var weapons = roundKills.Select(k => k.Weapon).Distinct();
            var headshotCount = roundKills.Count(k => k.IsHeadshot);
            var desc = $"{killCount}K — {string.Join(", ", weapons)}";
            if (headshotCount > 0) desc += $" ({headshotCount} HS)";

            highlights.Add(new Highlight
            {
                MatchId = match.MatchId,
                RoundNumber = group.Key,
                HighlightType = type,
                TickStart = roundKills.First().Tick,
                TickEnd = roundKills.Last().Tick,
                Description = desc
            });
        }

        return Task.FromResult(highlights);
    }
}
