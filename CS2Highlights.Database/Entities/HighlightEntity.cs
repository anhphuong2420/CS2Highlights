using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CS2Highlights.Core.Enums;

namespace CS2Highlights.Database.Entities;

[Table("Highlights")]
public class HighlightEntity
{
    [Key]
    public int Id { get; set; }

    public int MatchId { get; set; }
    public int? RoundId { get; set; }

    public HighlightType? HighlightType { get; set; }
    public LowlightType? LowlightType { get; set; }

    public int TickStart { get; set; }
    public int TickEnd { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ClipPath { get; set; }
    public RenderStatus RenderStatus { get; set; } = RenderStatus.Queued;

    public MatchEntity Match { get; set; } = null!;
    public RoundEntity? Round { get; set; }
    public ICollection<RenderJobEntity> RenderJobs { get; set; } = [];
}
