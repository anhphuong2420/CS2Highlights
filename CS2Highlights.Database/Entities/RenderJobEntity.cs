using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CS2Highlights.Core.Enums;

namespace CS2Highlights.Database.Entities;

[Table("RenderJobs")]
public class RenderJobEntity
{
    [Key]
    public int Id { get; set; }

    public int HighlightId { get; set; }

    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public RenderStatus Status { get; set; } = RenderStatus.Queued;
    public string? ClipPath { get; set; }
    public string? ErrorMessage { get; set; }

    public HighlightEntity Highlight { get; set; } = null!;
}
