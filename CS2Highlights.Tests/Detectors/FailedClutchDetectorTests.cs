using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class FailedClutchDetectorTests
{
    private FailedClutchDetector _detector = null!;
    private DetectionOptions _options = null!;
    private PlayerInfo _player = null!;

    [SetUp]
    public void SetUp()
    {
        _detector = new FailedClutchDetector();
        _options = new DetectionOptions { FailedClutchEnabled = true };
        _player = new PlayerInfo { SteamId = "P1", PlayerName = "TestPlayer", Team = "CounterTerrorist" };
    }

    // Helper: build a 1v2 scenario — TM1 dies, then P1 is last alive vs E1+E2, then T wins
    private ParsedMatch BuildFailedClutch1v2(TeamSide winnerSide)
    {
        var allPlayers = new List<PlayerInfo>
        {
            _player,
            new() { SteamId = "TM1", PlayerName = "Teammate", Team = "CounterTerrorist" },
            new() { SteamId = "E1",  PlayerName = "Enemy1",   Team = "Terrorist" },
            new() { SteamId = "E2",  PlayerName = "Enemy2",   Team = "Terrorist" },
        };
        var round = new Round { RoundNumber = 1, TickStart = 1000, TickEnd = 8000, WinnerSide = winnerSide };
        var kills = new List<KillEvent>
        {
            new() { RoundNumber = 1, Tick = 2000, SteamId = "E1", VictimSteamId = "TM1" }, // TM1 dies → P1 last alive
            new() { RoundNumber = 1, Tick = 3000, SteamId = "E1", VictimSteamId = "P1"  }, // P1 dies
        };
        return new ParsedMatch
        {
            MatchId = "test", SelectedPlayer = _player,
            AllPlayers = allPlayers, Rounds = [round], Kills = kills
        };
    }

    [Test]
    public async Task LastAlive_RoundLost_Detected()
    {
        var match = BuildFailedClutch1v2(winnerSide: TeamSide.T);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LowlightType, Is.EqualTo(LowlightType.FailedClutch));
        Assert.That(result[0].Description, Does.Contain("1v2"));
    }

    [Test]
    public async Task LastAlive_RoundWon_Not_Detected()
    {
        // Same scenario but CT wins → ClutchDetector territory, not a lowlight
        var match = BuildFailedClutch1v2(winnerSide: TeamSide.CT);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Player_dies_before_becoming_last_alive_not_detected()
    {
        var allPlayers = new List<PlayerInfo>
        {
            _player,
            new() { SteamId = "TM1", PlayerName = "Teammate", Team = "CounterTerrorist" },
            new() { SteamId = "E1",  PlayerName = "Enemy",    Team = "Terrorist" },
        };
        var round = new Round { RoundNumber = 1, TickStart = 1000, TickEnd = 8000, WinnerSide = TeamSide.T };
        var kills = new List<KillEvent>
        {
            new() { RoundNumber = 1, Tick = 2000, SteamId = "E1", VictimSteamId = "P1"  }, // P1 dies first
            new() { RoundNumber = 1, Tick = 3000, SteamId = "E1", VictimSteamId = "TM1" },
        };
        var match = new ParsedMatch
        {
            MatchId = "test", SelectedPlayer = _player,
            AllPlayers = allPlayers, Rounds = [round], Kills = kills
        };
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task AllPlayers_empty_skips_detection()
    {
        var match = new ParsedMatch
        {
            MatchId = "test", SelectedPlayer = _player,
            AllPlayers = [],
            Rounds = [new Round { RoundNumber = 1, TickStart = 1000, TickEnd = 8000, WinnerSide = TeamSide.T }],
            Kills = []
        };
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Disabled_returns_empty()
    {
        _options.FailedClutchEnabled = false;
        var match = BuildFailedClutch1v2(winnerSide: TeamSide.T);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Round_with_no_kills_not_detected()
    {
        var allPlayers = new List<PlayerInfo>
        {
            _player,
            new() { SteamId = "TM1", Team = "CounterTerrorist", PlayerName = "TM1" },
            new() { SteamId = "E1",  Team = "Terrorist",         PlayerName = "E1" },
        };
        var round = new Round { RoundNumber = 1, TickStart = 1000, TickEnd = 8000, WinnerSide = TeamSide.T };
        var match = new ParsedMatch
        {
            MatchId = "test", SelectedPlayer = _player,
            AllPlayers = allPlayers, Rounds = [round], Kills = []
        };
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }
}
