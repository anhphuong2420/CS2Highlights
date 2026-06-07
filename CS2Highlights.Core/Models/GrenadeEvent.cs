using CS2Highlights.Core.Enums;

namespace CS2Highlights.Core.Models;

public class GrenadeEvent : PlayerEvent
{
    public GrenadeType GrenadeType { get; set; }
    public int DamageToEnemies { get; set; }
    public int DamageToTeam { get; set; }
    public int EnemiesBlinded { get; set; }
    public int TeammatesBlinded { get; set; }
}
