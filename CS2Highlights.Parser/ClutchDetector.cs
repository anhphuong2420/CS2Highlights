using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class ClutchDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();

        if (!options.ClutchEnabled || match.AllPlayers.Count == 0)
            return Task.FromResult(highlights);

        foreach (var round in match.Rounds)
        {
            var playerTeam = GetTeamInRound(match.SelectedPlayer.Team, round.RoundNumber);

            var teammatesAlive = match.AllPlayers
                .Where(p => p.SteamId != match.SelectedPlayer.SteamId
                         && GetTeamInRound(p.Team, round.RoundNumber) == playerTeam)
                .Select(p => p.SteamId)
                .ToHashSet();

            var enemiesAlive = match.AllPlayers
                .Where(p => GetTeamInRound(p.Team, round.RoundNumber) != playerTeam)
                .Select(p => p.SteamId)
                .ToHashSet();

            bool selectedAlive = true;
            int clutchStartTick = -1;
            int clutchEnemyCount = 0;

            var roundKills = match.Kills
                .Where(k => k.RoundNumber == round.RoundNumber)
                .OrderBy(k => k.Tick);

            foreach (var kill in roundKills)
            {
                if (kill.VictimSteamId == match.SelectedPlayer.SteamId)
                {
                    selectedAlive = false;
                    break;
                }

                teammatesAlive.Remove(kill.VictimSteamId);
                enemiesAlive.Remove(kill.VictimSteamId);

                // Detect the moment selectedPlayer becomes last alive
                if (clutchStartTick < 0 && teammatesAlive.Count == 0 && enemiesAlive.Count > 0)
                {
                    clutchStartTick = kill.Tick;
                    clutchEnemyCount = enemiesAlive.Count;
                }
            }

            if (!selectedAlive || clutchStartTick < 0) continue;
            if (clutchEnemyCount < options.OutnumberedWinMinEnemies) continue;

            var playerSide = playerTeam == "CounterTerrorist" ? TeamSide.CT : TeamSide.T;
            if (round.WinnerSide != playerSide) continue;

            highlights.Add(new Highlight
            {
                MatchId = match.MatchId,
                RoundNumber = round.RoundNumber,
                HighlightType = HighlightType.Clutch,
                TickStart = clutchStartTick,
                TickEnd = round.TickEnd,
                Description = $"1v{clutchEnemyCount} clutch win (R{round.RoundNumber})"
            });
        }

        return Task.FromResult(highlights);
    }

    // CS2 competitive: 12-round halves; OT uses 3-round halves
    private static string GetTeamInRound(string baseTeam, int roundNumber)
    {
        bool switched;
        if (roundNumber <= 12)      switched = false;
        else if (roundNumber <= 24) switched = true;
        else                        switched = ((roundNumber - 25) / 3) % 2 == 1;

        if (!switched) return baseTeam;
        return baseTeam == "CounterTerrorist" ? "Terrorist" : "CounterTerrorist";
    }
}
