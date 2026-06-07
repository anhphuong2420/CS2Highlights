using CS2Highlights.Core.Enums;

namespace CS2Highlights.Core.Models;

public class Highlight
{
    public int HighlightId { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public HighlightType? HighlightType { get; set; }
    public LowlightType? LowlightType { get; set; }
    public int TickStart { get; set; }
    public int TickEnd { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ClipPath { get; set; }
    public RenderStatus RenderStatus { get; set; } = RenderStatus.Queued;

    public bool IsLowlight => LowlightType.HasValue;
}
