using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class MultiKillDetectorTests
{
    private MultiKillDetector _detector = null!;
    private DetectionOptions _options = null!;
    private PlayerInfo _player = null!;

    [SetUp]
    public void SetUp()
    {
        _detector = new MultiKillDetector();
        _options = new DetectionOptions { MultiKillEnabled = true, MultiKillMinKills = 3 };
        _player = new PlayerInfo { SteamId = "111", PlayerName = "TestPlayer" };
    }

    private ParsedMatch BuildMatch(List<KillEvent> kills) => new()
    {
        MatchId = "abc",
        SelectedPlayer = _player,
        Kills = kills
    };

    private KillEvent Kill(int round, int tick, string weapon = "ak47", bool hs = false)
        => new() { RoundNumber = round, Tick = tick, SteamId = "111", VictimSteamId = "999", Weapon = weapon, IsHeadshot = hs };

    // ---- Threshold ----

    [Test]
    public async Task Below_threshold_no_highlight()
    {
        var match = BuildMatch([Kill(1, 100), Kill(1, 200)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Triple_kill_produces_MultiKill3()
    {
        var match = BuildMatch([Kill(1, 100), Kill(1, 200), Kill(1, 300)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].HighlightType, Is.EqualTo(HighlightType.MultiKill3));
    }

    [Test]
    public async Task Four_kills_produces_MultiKill4()
    {
        var match = BuildMatch([Kill(1, 100), Kill(1, 200), Kill(1, 300), Kill(1, 400)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result[0].HighlightType, Is.EqualTo(HighlightType.MultiKill4));
    }

    [Test]
    public async Task Five_kills_produces_MultiKill5()
    {
        var match = BuildMatch([Kill(1, 100), Kill(1, 200), Kill(1, 300), Kill(1, 400), Kill(1, 500)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result[0].HighlightType, Is.EqualTo(HighlightType.MultiKill5));
    }

    // ---- Tick range ----

    [Test]
    public async Task Tick_start_and_end_match_first_and_last_kill()
    {
        var match = BuildMatch([Kill(1, 500), Kill(1, 800), Kill(1, 1200)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result[0].TickStart, Is.EqualTo(500));
        Assert.That(result[0].TickEnd, Is.EqualTo(1200));
    }

    // ---- Round isolation ----

    [Test]
    public async Task Kills_across_rounds_not_combined()
    {
        // 2 kills in R1 + 2 kills in R2 = should produce 0 highlights (neither round hits 3)
        var match = BuildMatch([Kill(1, 100), Kill(1, 200), Kill(2, 300), Kill(2, 400)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Separate_triple_kills_in_different_rounds()
    {
        var match = BuildMatch([
            Kill(1, 100), Kill(1, 200), Kill(1, 300),
            Kill(3, 500), Kill(3, 600), Kill(3, 700)
        ]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(h => h.RoundNumber), Is.EquivalentTo(new[] { 1, 3 }));
    }

    // ---- Other players' kills ignored ----

    [Test]
    public async Task Other_players_kills_not_counted()
    {
        var match = BuildMatch([
            new KillEvent { RoundNumber = 1, Tick = 100, SteamId = "999", VictimSteamId = "111", Weapon = "ak47" },
            new KillEvent { RoundNumber = 1, Tick = 200, SteamId = "999", VictimSteamId = "222", Weapon = "ak47" },
            new KillEvent { RoundNumber = 1, Tick = 300, SteamId = "999", VictimSteamId = "333", Weapon = "ak47" },
        ]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- Disabled ----

    [Test]
    public async Task Disabled_option_returns_empty()
    {
        _options.MultiKillEnabled = false;
        var match = BuildMatch([Kill(1, 100), Kill(1, 200), Kill(1, 300)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- Description ----

    [Test]
    public async Task Description_includes_headshot_count()
    {
        var match = BuildMatch([Kill(1, 100, "ak47", true), Kill(1, 200, "ak47", true), Kill(1, 300, "ak47", false)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result[0].Description, Does.Contain("2 HS"));
    }
}
