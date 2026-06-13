using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class EntryFragDetectorTests
{
    private EntryFragDetector _detector = null!;
    private DetectionOptions _options = null!;
    private PlayerInfo _player = null!;

    [SetUp]
    public void SetUp()
    {
        _detector = new EntryFragDetector();
        _options = new DetectionOptions { EntryFragEnabled = true, EntryFragTimeSeconds = 8 };
        _player = new PlayerInfo { SteamId = "P1", PlayerName = "TestPlayer", Team = "CounterTerrorist" };
    }

    private static Round R(int num, int tickStart = 1000)
        => new() { RoundNumber = num, TickStart = tickStart, TickEnd = tickStart + 10000 };

    private static KillEvent Kill(int round, int tick, string killer, string weapon = "ak47", bool hs = false)
        => new() { RoundNumber = round, Tick = tick, SteamId = killer, VictimSteamId = "E1", Weapon = weapon, IsHeadshot = hs };

    private ParsedMatch BuildMatch(List<KillEvent> kills, List<Round>? rounds = null) => new()
    {
        MatchId        = "test",
        SelectedPlayer = _player,
        Kills          = kills,
        Rounds         = rounds ?? [R(1)]
    };

    [Test]
    public async Task Player_gets_first_kill_within_time_produces_EntryFrag()
    {
        // Round starts at tick 1000, kill at 1100 = 100 ticks = ~1.5s (well within 8s = 512 ticks)
        var match = BuildMatch([Kill(1, 1100, "P1")], [R(1, tickStart: 1000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].HighlightType, Is.EqualTo(HighlightType.EntryFrag));
    }

    [Test]
    public async Task Other_player_gets_first_kill_not_detected()
    {
        // Enemy E2 gets first kill, then player gets second kill
        var match = BuildMatch(
            [Kill(1, 1100, "E2"), Kill(1, 1200, "P1")],
            [R(1, tickStart: 1000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Player_first_kill_but_too_late_not_detected()
    {
        // Round starts at 1000, threshold = 8s * 64 = 512 ticks, kill at 1000+600 = 1600 > threshold
        var match = BuildMatch([Kill(1, 1600, "P1")], [R(1, tickStart: 1000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Exactly_at_threshold_is_detected()
    {
        // threshold = 8 * 64 = 512; kill at 1000 + 512 = 1512 (exactly on threshold)
        var match = BuildMatch([Kill(1, 1512, "P1")], [R(1, tickStart: 1000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task No_kills_in_round_no_highlight()
    {
        var match = BuildMatch([], [R(1)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Disabled_option_returns_empty()
    {
        _options.EntryFragEnabled = false;
        var match = BuildMatch([Kill(1, 1100, "P1")], [R(1, tickStart: 1000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Description_includes_headshot()
    {
        var match = BuildMatch([Kill(1, 1100, "P1", hs: true)], [R(1, tickStart: 1000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result[0].Description, Does.Contain("(HS)"));
    }

    [Test]
    public async Task Multiple_rounds_each_entry_frag_detected_separately()
    {
        var match = BuildMatch(
            [Kill(1, 1100, "P1"), Kill(2, 2100, "P1")],
            [R(1, tickStart: 1000), R(2, tickStart: 2000)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(2));
    }
}
