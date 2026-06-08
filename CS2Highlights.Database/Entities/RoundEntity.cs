using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CS2Highlights.Core.Enums;

namespace CS2Highlights.Database.Entities;

[Table("Rounds")]
public class RoundEntity
{
    [Key]
    public int Id { get; set; }

    public int MatchId { get; set; }

    public int RoundNumber { get; set; }
    public int TickStart { get; set; }
    public int TickEnd { get; set; }
    public TeamSide WinnerSide { get; set; }

    public MatchEntity Match { get; set; } = null!;
    public ICollection<KillEventEntity> KillEvents { get; set; } = [];
    public ICollection<GrenadeEventEntity> GrenadeEvents { get; set; } = [];
    public ICollection<HighlightEntity> Highlights { get; set; } = [];
}
