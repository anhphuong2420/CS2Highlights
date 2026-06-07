namespace CS2Highlights.Core.Models;

public class ParsedMatch
{
    public string MatchId { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Score { get; set; } = string.Empty;
    public string DemoPath { get; set; } = string.Empty;

    public List<Round> Rounds { get; set; } = [];
    public List<KillEvent> Kills { get; set; } = [];
    public List<DeathEvent> Deaths { get; set; } = [];
    public List<GrenadeEvent> Grenades { get; set; } = [];
    public List<ClutchEvent> Clutches { get; set; } = [];
}
