using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface IDemoParser
{
    Task<List<PlayerInfo>> ReadPlayersAsync(string demoPath);
    Task<ParsedMatch> ParseAsync(string demoPath, PlayerInfo selectedPlayer);
}
