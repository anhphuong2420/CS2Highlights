using CS2Highlights.Core.Enums;

namespace CS2Highlights.Core.Models;

public class RenderProgress
{
    public int JobId { get; set; }       // 0 = log-only message, no state change
    public int HighlightId { get; set; }
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public RenderStatus Status { get; set; }
    public string? LogMessage { get; set; }
    public string? ClipPath { get; set; }
}
