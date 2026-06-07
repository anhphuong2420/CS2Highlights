namespace CS2Highlights.Core.Models;

public class RenderJob
{
    public int JobId { get; set; }
    public string DemoPath { get; set; } = string.Empty;
    public int TickStart { get; set; }
    public int TickEnd { get; set; }
    public string PlayerSteamId { get; set; } = string.Empty;
    public int HighlightId { get; set; }
    public RenderSettings Settings { get; set; } = new();
}
