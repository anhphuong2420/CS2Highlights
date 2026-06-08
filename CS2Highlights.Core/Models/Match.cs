namespace CS2Highlights.Core.Models;

public class Match
{
    public string MatchId { get; set; } = string.Empty;       // unique ID from demo header
    public string DemoPath { get; set; } = string.Empty;
    public string DemoFileName { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string SelectedPlayerSteamId { get; set; } = string.Empty;
    public string SelectedPlayerName { get; set; } = string.Empty;
    public DateTime? ParsedAt { get; set; }
}
