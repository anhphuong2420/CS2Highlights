using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class FriendlyFireDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();
        if (!options.FriendlyFireEnabled)
            return Task.FromResult(highlights);

        foreach (var group in match.Grenades.Where(g => g.DamageToTeam > 0).GroupBy(g => g.RoundNumber))
        {
            var totalDamage = group.Sum(g => g.DamageToTeam);
            if (totalDamage < options.FriendlyFireDamageThreshold) continue;

            var worst = group.OrderByDescending(g => g.DamageToTeam).First();
            highlights.Add(new Highlight
            {
                MatchId      = match.MatchId,
                RoundNumber  = group.Key,
                LowlightType = LowlightType.FriendlyFire,
                TickStart    = worst.Tick,
                TickEnd      = worst.Tick + 64,
                Description  = $"{totalDamage} friendly-fire damage in R{group.Key}"
            });
        }

        return Task.FromResult(highlights);
    }
}
