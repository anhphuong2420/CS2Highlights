using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS2Highlights.Database.Entities;

[Table("KillEvents")]
public class KillEventEntity
{
    [Key]
    public int Id { get; set; }

    public int MatchId { get; set; }
    public int RoundId { get; set; }

    public int Tick { get; set; }
    public string KillerSteamId { get; set; } = string.Empty;
    public string VictimSteamId { get; set; } = string.Empty;
    public string Weapon { get; set; } = string.Empty;
    public bool IsHeadshot { get; set; }
    public bool IsWallbang { get; set; }
    public bool IsNoscope { get; set; }

    public MatchEntity Match { get; set; } = null!;
    public RoundEntity Round { get; set; } = null!;
}
