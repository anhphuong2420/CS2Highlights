using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface IDemoParser
{
    Task<ParsedMatch> ParseAsync(string demoPath, string steamId);
}
