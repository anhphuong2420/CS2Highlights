namespace CS2Highlights.Parser;

internal static class TeamHelper
{
    // CS2 competitive: 12-round halves; OT uses 3-round halves
    internal static string GetTeamInRound(string baseTeam, int roundNumber)
    {
        bool switched;
        if (roundNumber <= 12)      switched = false;
        else if (roundNumber <= 24) switched = true;
        else                        switched = ((roundNumber - 25) / 3) % 2 == 1;

        if (!switched) return baseTeam;
        return baseTeam == "CounterTerrorist" ? "Terrorist" : "CounterTerrorist";
    }
}
