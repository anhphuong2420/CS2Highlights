using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class EntryFragDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();
        if (!options.EntryFragEnabled)
            return Task.FromResult(highlights);

        var roundMap = match.Rounds.ToDictionary(r => r.RoundNumber);

        foreach (var group in match.Kills.GroupBy(k => k.RoundNumber))
        {
            if (!roundMap.TryGetValue(group.Key, out var round)) continue;

            var firstKill = group.OrderBy(k => k.Tick).First();
            if (firstKill.SteamId != match.SelectedPlayer.SteamId) continue;
            if (firstKill.Tick - round.TickStart > options.EntryFragTimeSeconds * 64) continue;

            var desc = $"Entry frag with {firstKill.Weapon}";
            if (firstKill.IsHeadshot) desc += " (HS)";

            highlights.Add(new Highlight
            {
                MatchId       = match.MatchId,
                RoundNumber   = round.RoundNumber,
                HighlightType = HighlightType.EntryFrag,
                TickStart     = firstKill.Tick,
                TickEnd       = firstKill.Tick + 64,
                Description   = desc
            });
        }

        return Task.FromResult(highlights);
    }
}
