using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class GrenadeDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();

        foreach (var g in match.Grenades)
        {
            if (g.GrenadeType == GrenadeType.Flash
                && options.TeamFlashEnabled
                && g.TeammatesBlinded >= options.TeamFlashMinTeammatesBlinded)
            {
                highlights.Add(new Highlight
                {
                    MatchId      = match.MatchId,
                    RoundNumber  = g.RoundNumber,
                    LowlightType = LowlightType.TeamFlash,
                    TickStart    = g.Tick,
                    TickEnd      = g.Tick + 192,
                    Description  = $"Flashed {g.TeammatesBlinded} teammate(s) in R{g.RoundNumber}"
                });
            }

            if ((g.GrenadeType == GrenadeType.Molotov || g.GrenadeType == GrenadeType.Incendiary)
                && options.TeamMolotovEnabled
                && g.DamageToTeam > 0)
            {
                highlights.Add(new Highlight
                {
                    MatchId      = match.MatchId,
                    RoundNumber  = g.RoundNumber,
                    LowlightType = LowlightType.TeamMolotov,
                    TickStart    = g.Tick,
                    TickEnd      = g.Tick + 320,
                    Description  = $"Team molotov — {g.DamageToTeam} damage to team in R{g.RoundNumber}"
                });
            }

            if (g.GrenadeType == GrenadeType.HE
                && options.WastedGrenadeEnabled
                && g.DamageToEnemies == 0)
            {
                highlights.Add(new Highlight
                {
                    MatchId      = match.MatchId,
                    RoundNumber  = g.RoundNumber,
                    LowlightType = LowlightType.WastedGrenade,
                    TickStart    = g.Tick,
                    TickEnd      = g.Tick + 64,
                    Description  = $"HE grenade — 0 enemy damage in R{g.RoundNumber}"
                });
            }

            if (g.GrenadeType == GrenadeType.HE
                && options.LowDamageGrenadeEnabled
                && g.DamageToEnemies > 0
                && g.DamageToEnemies < options.LowDamageGrenadeThreshold)
            {
                highlights.Add(new Highlight
                {
                    MatchId      = match.MatchId,
                    RoundNumber  = g.RoundNumber,
                    LowlightType = LowlightType.LowDamageGrenade,
                    TickStart    = g.Tick,
                    TickEnd      = g.Tick + 64,
                    Description  = $"HE grenade — only {g.DamageToEnemies} enemy damage in R{g.RoundNumber}"
                });
            }
        }

        return Task.FromResult(highlights);
    }
}
