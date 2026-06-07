namespace CS2Highlights.Core.Models;

public class DeathEvent : PlayerEvent
{
    public string KillerSteamId { get; set; } = string.Empty;
    public float TimeIntoRound { get; set; }
}
