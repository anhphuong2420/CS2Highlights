using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CS2Highlights.Core.Enums;

namespace CS2Highlights.Database.Entities;

[Table("GrenadeEvents")]
public class GrenadeEventEntity
{
    [Key]
    public int Id { get; set; }

    public int MatchId { get; set; }
    public int RoundId { get; set; }

    public int Tick { get; set; }
    public string ThrowerSteamId { get; set; } = string.Empty;
    public GrenadeType GrenadeType { get; set; }
    public int DmgToEnemies { get; set; }
    public int DmgToTeam { get; set; }
    public int EnemiesBlinded { get; set; }
    public int TeammatesBlinded { get; set; }

    public MatchEntity Match { get; set; } = null!;
    public RoundEntity Round { get; set; } = null!;
}
