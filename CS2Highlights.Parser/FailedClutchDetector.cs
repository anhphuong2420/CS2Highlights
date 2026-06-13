using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class FailedClutchDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();
        if (!options.FailedClutchEnabled || match.AllPlayers.Count == 0)
            return Task.FromResult(highlights);

        foreach (var round in match.Rounds)
        {
            var playerTeam = TeamHelper.GetTeamInRound(match.SelectedPlayer.Team, round.RoundNumber);

            var teammatesAlive = match.AllPlayers
                .Where(p => p.SteamId != match.SelectedPlayer.SteamId
                         && TeamHelper.GetTeamInRound(p.Team, round.RoundNumber) == playerTeam)
                .Select(p => p.SteamId)
                .ToHashSet();

            var enemiesAlive = match.AllPlayers
                .Where(p => TeamHelper.GetTeamInRound(p.Team, round.RoundNumber) != playerTeam)
                .Select(p => p.SteamId)
                .ToHashSet();

            int clutchStartTick  = -1;
            int clutchEnemyCount = 0;

            foreach (var kill in match.Kills.Where(k => k.RoundNumber == round.RoundNumber).OrderBy(k => k.Tick))
            {
                if (kill.VictimSteamId == match.SelectedPlayer.SteamId)
                    break;

                teammatesAlive.Remove(kill.VictimSteamId);
                enemiesAlive.Remove(kill.VictimSteamId);

                if (clutchStartTick < 0 && teammatesAlive.Count == 0 && enemiesAlive.Count > 0)
                {
                    clutchStartTick  = kill.Tick;
                    clutchEnemyCount = enemiesAlive.Count;
                }
            }

            if (clutchStartTick < 0) continue;

            var playerSide = playerTeam == "CounterTerrorist" ? TeamSide.CT : TeamSide.T;
            if (round.WinnerSide == playerSide) continue; // won = Clutch highlight, not lowlight

            highlights.Add(new Highlight
            {
                MatchId      = match.MatchId,
                RoundNumber  = round.RoundNumber,
                LowlightType = LowlightType.FailedClutch,
                TickStart    = clutchStartTick,
                TickEnd      = round.TickEnd,
                Description  = $"1v{clutchEnemyCount} — round lost (R{round.RoundNumber})"
            });
        }

        return Task.FromResult(highlights);
    }
}
