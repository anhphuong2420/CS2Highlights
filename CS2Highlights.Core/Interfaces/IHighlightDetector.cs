using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface IHighlightDetector
{
    Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options);
}
