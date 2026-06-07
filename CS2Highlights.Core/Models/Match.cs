namespace CS2Highlights.Core.Models;

public class Match
{
    public string MatchId { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Score { get; set; } = string.Empty;
    public string DemoPath { get; set; } = string.Empty;
    public DateTime? ParsedAt { get; set; }
}
