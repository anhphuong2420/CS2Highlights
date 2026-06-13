using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class DeathStreakDetector : IHighlightDetector
{
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
    {
        var highlights = new List<Highlight>();
        if (!options.DeathStreakEnabled)
            return Task.FromResult(highlights);

        var deathRoundSet  = match.Deaths.Select(d => d.RoundNumber).ToHashSet();
        var deathTickByRound = match.Deaths.ToDictionary(d => d.RoundNumber, d => d.Tick);
        var roundNumbers = match.Rounds.OrderBy(r => r.RoundNumber).Select(r => r.RoundNumber).ToList();

        int i = 0;
        while (i < roundNumbers.Count)
        {
            if (!deathRoundSet.Contains(roundNumbers[i])) { i++; continue; }

            int start = i;
            while (i < roundNumbers.Count && deathRoundSet.Contains(roundNumbers[i]))
                i++;

            int streakLen = i - start;
            if (streakLen < options.DeathStreakCount) continue;

            var startRound = roundNumbers[start];
            var endRound   = roundNumbers[i - 1];

            highlights.Add(new Highlight
            {
                MatchId      = match.MatchId,
                RoundNumber  = startRound,
                LowlightType = LowlightType.DeathStreak,
                TickStart    = deathTickByRound[startRound],
                TickEnd      = deathTickByRound[endRound],
                Description  = $"{streakLen} consecutive deaths (R{startRound}–R{endRound})"
            });
        }

        return Task.FromResult(highlights);
    }
}
