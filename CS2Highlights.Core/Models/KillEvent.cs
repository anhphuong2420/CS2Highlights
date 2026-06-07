namespace CS2Highlights.Core.Models;

public class KillEvent : PlayerEvent
{
    public string VictimSteamId { get; set; } = string.Empty;
    public string Weapon { get; set; } = string.Empty;
    public bool IsHeadshot { get; set; }
    public bool IsWallbang { get; set; }
    public bool IsNoscope { get; set; }
}
