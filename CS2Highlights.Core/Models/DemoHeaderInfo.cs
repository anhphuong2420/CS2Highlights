namespace CS2Highlights.Core.Models;

public class DemoHeaderInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public List<PlayerInfo> Players { get; set; } = new();
}
