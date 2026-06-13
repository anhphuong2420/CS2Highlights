using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class ClutchDetectorTests
{
    private ClutchDetector _detector = null!;
    private DetectionOptions _options = null!;

    private const string PlayerSteamId = "1";
    private const string CT2 = "2", CT3 = "3", CT4 = "4", CT5 = "5";
    private const string T1 = "10", T2 = "11", T3 = "12", T4 = "13", T5 = "14";

    [SetUp]
    public void SetUp()
    {
        _detector = new ClutchDetector();
        _options = new DetectionOptions { ClutchEnabled = true, OutnumberedWinMinEnemies = 2 };
    }

    // CT team: player + CT2..CT5 (4 teammates)
    // T  team: T1..T5 (5 enemies)
    private ParsedMatch BuildCtMatch(List<KillEvent> kills, int roundNumber = 1, TeamSide winner = TeamSide.CT)
    {
        var player = new PlayerInfo { SteamId = PlayerSteamId, PlayerName = "P", Team = "CounterTerrorist" };
        return new ParsedMatch
        {
            MatchId = "abc",
            SelectedPlayer = player,
            AllPlayers =
            [
                player,
                new() { SteamId = CT2, Team = "CounterTerrorist" },
                new() { SteamId = CT3, Team = "CounterTerrorist" },
                new() { SteamId = CT4, Team = "CounterTerrorist" },
                new() { SteamId = CT5, Team = "CounterTerrorist" },
                new() { SteamId = T1, Team = "Terrorist" },
                new() { SteamId = T2, Team = "Terrorist" },
                new() { SteamId = T3, Team = "Terrorist" },
                new() { SteamId = T4, Team = "Terrorist" },
                new() { SteamId = T5, Team = "Terrorist" },
            ],
            Rounds = [new Round { RoundNumber = roundNumber, TickStart = 0, TickEnd = 5000, WinnerSide = winner }],
            Kills = kills
        };
    }

    private static KillEvent Kill(string killer, string victim, int tick)
        => new() { SteamId = killer, VictimSteamId = victim, Tick = tick, RoundNumber = 1, Weapon = "ak47" };

    // ---- 1v2 clutch win ----

    [Test]
    public async Task OneVsTwo_win_produces_clutch_highlight()
    {
        // CT4, CT3, CT2, CT5 die → player is last alive 1v5 → kills T1,T2,T3,T4,T5
        var kills = new List<KillEvent>
        {
            Kill(T1, CT2, 100),
            Kill(T2, CT3, 200),
            Kill(T3, CT4, 300),
            Kill(T4, CT5, 400),    // ← player now last alive, 1v5 starts here
            Kill(PlayerSteamId, T1, 500),
            Kill(PlayerSteamId, T2, 600),
            Kill(PlayerSteamId, T3, 700),
            Kill(PlayerSteamId, T4, 800),
            Kill(PlayerSteamId, T5, 900),
        };
        var match = BuildCtMatch(kills, winner: TeamSide.CT);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].HighlightType, Is.EqualTo(HighlightType.Clutch));
        Assert.That(result[0].Description, Does.Contain("1v5"));
    }

    [Test]
    public async Task OneVsTwo_clutch_tick_start_is_when_last_teammate_died()
    {
        var kills = new List<KillEvent>
        {
            Kill(T1, CT2, 100),
            Kill(T2, CT3, 200),
            Kill(T3, CT4, 300),
            Kill(T4, CT5, 400),  // ← clutch starts here (tick 400)
            Kill(PlayerSteamId, T1, 500),
            Kill(PlayerSteamId, T2, 600),
            Kill(PlayerSteamId, T3, 700),
            Kill(PlayerSteamId, T4, 800),
            Kill(PlayerSteamId, T5, 900),
        };
        var match = BuildCtMatch(kills, winner: TeamSide.CT);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result[0].TickStart, Is.EqualTo(400));
    }

    // ---- No highlight when player dies ----

    [Test]
    public async Task Player_dies_before_clutch_no_highlight()
    {
        var kills = new List<KillEvent>
        {
            Kill(T1, CT2, 100),
            Kill(T2, CT3, 200),
            Kill(T3, PlayerSteamId, 300), // player dies
            Kill(T4, CT4, 400),
            Kill(T5, CT5, 500),
        };
        var match = BuildCtMatch(kills, winner: TeamSide.T);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- Round lost = no Clutch highlight ----

    [Test]
    public async Task Round_lost_no_clutch_highlight()
    {
        var kills = new List<KillEvent>
        {
            Kill(T1, CT2, 100),
            Kill(T2, CT3, 200),
            Kill(T3, CT4, 300),
            Kill(T4, CT5, 400),
            Kill(T5, PlayerSteamId, 500), // player dies → T wins
        };
        var match = BuildCtMatch(kills, winner: TeamSide.T);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- OutnumberedWinMinEnemies threshold ----

    [Test]
    public async Task Below_min_enemies_threshold_no_highlight()
    {
        // 1v1 when threshold is 2
        var kills = new List<KillEvent>
        {
            Kill(T1, CT2, 100),
            Kill(T2, CT3, 200),
            Kill(T3, CT4, 300),
            Kill(T4, CT5, 400),
            Kill(T5, T2, 410), // enemies also kill each other
            Kill(T5, T3, 420),
            Kill(T5, T4, 430),
            Kill(T5, T5, 440), // 4 enemies die too — only T1 remains → 1v1
            // T1 is still alive though... let me redo
        };
        // Simpler: just have T2,T3,T4,T5 killed before player becomes last alive
        var kills2 = new List<KillEvent>
        {
            Kill(PlayerSteamId, T2, 100),
            Kill(PlayerSteamId, T3, 200),
            Kill(PlayerSteamId, T4, 300),
            Kill(PlayerSteamId, T5, 400), // 4 enemies dead → only T1 left
            Kill(T1, CT2, 500),
            Kill(T1, CT3, 600),
            Kill(T1, CT4, 700),
            Kill(T1, CT5, 800), // player is last alive, 1v1 — below threshold
            Kill(PlayerSteamId, T1, 900),
        };
        var match = BuildCtMatch(kills2, winner: TeamSide.CT);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- Disabled ----

    [Test]
    public async Task Disabled_option_returns_empty()
    {
        _options.ClutchEnabled = false;
        var kills = new List<KillEvent>
        {
            Kill(T1, CT2, 100), Kill(T2, CT3, 200), Kill(T3, CT4, 300), Kill(T4, CT5, 400),
            Kill(PlayerSteamId, T1, 500), Kill(PlayerSteamId, T2, 600),
            Kill(PlayerSteamId, T3, 700), Kill(PlayerSteamId, T4, 800), Kill(PlayerSteamId, T5, 900),
        };
        var match = BuildCtMatch(kills, winner: TeamSide.CT);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- No AllPlayers = no detection ----

    [Test]
    public async Task Empty_all_players_skips_detection()
    {
        var match = new ParsedMatch
        {
            MatchId = "abc",
            SelectedPlayer = new PlayerInfo { SteamId = PlayerSteamId, Team = "CounterTerrorist" },
            AllPlayers = [],
            Rounds = [new Round { RoundNumber = 1, WinnerSide = TeamSide.CT }],
            Kills = [Kill(T1, CT2, 100)]
        };
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }
}
