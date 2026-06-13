using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.Parser;

public class BombDropDetector : IHighlightDetector
{
    // Requires bomb carrier tracking (BombPickup/BombDropped events) in DemoParser — not yet implemented.
    public Task<List<Highlight>> DetectAsync(ParsedMatch match, DetectionOptions options)
        => Task.FromResult(new List<Highlight>());
}
