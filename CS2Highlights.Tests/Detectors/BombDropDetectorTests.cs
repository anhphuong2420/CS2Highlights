using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class BombDropDetectorTests
{
    [Test]
    public async Task Always_returns_empty_pending_bomb_carrier_tracking()
    {
        var detector = new BombDropDetector();
        var match = new ParsedMatch
        {
            MatchId = "test",
            SelectedPlayer = new PlayerInfo { SteamId = "P1", Team = "Terrorist" },
            Rounds = [new Round { RoundNumber = 1, TickStart = 1000, TickEnd = 8000, WinnerSide = Core.Enums.TeamSide.CT }],
            Deaths = [new DeathEvent { RoundNumber = 1, Tick = 5000, SteamId = "P1" }]
        };
        var result = await detector.DetectAsync(match, new DetectionOptions());
        Assert.That(result, Is.Empty);
    }
}
