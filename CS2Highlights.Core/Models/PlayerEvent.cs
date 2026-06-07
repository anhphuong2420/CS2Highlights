namespace CS2Highlights.Core.Models;

public abstract class PlayerEvent
{
    public int Tick { get; set; }
    public string SteamId { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
}
