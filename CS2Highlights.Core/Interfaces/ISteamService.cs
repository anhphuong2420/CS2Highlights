using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface ISteamService
{
    Task<List<MatchInfo>> GetMatchesAsync(string steamId);
    Task<string> DownloadDemoAsync(MatchInfo matchInfo);
}
