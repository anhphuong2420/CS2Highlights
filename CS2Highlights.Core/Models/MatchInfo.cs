namespace CS2Highlights.Core.Models;

public class MatchInfo
{
    public string ShareCode { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Score { get; set; } = string.Empty;
    public string DemoUrl { get; set; } = string.Empty;
}
