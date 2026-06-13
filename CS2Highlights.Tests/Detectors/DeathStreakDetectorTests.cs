using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class DeathStreakDetectorTests
{
    private DeathStreakDetector _detector = null!;
    private DetectionOptions _options = null!;
    private PlayerInfo _player = null!;

    [SetUp]
    public void SetUp()
    {
        _detector = new DeathStreakDetector();
        _options = new DetectionOptions { DeathStreakEnabled = true, DeathStreakCount = 3 };
        _player = new PlayerInfo { SteamId = "P1", PlayerName = "TestPlayer" };
    }

    private ParsedMatch BuildMatch(IEnumerable<int> deathRounds, IEnumerable<int> allRoundNumbers) => new()
    {
        MatchId        = "test",
        SelectedPlayer = _player,
        Deaths = deathRounds.Select(r => new DeathEvent
            { RoundNumber = r, Tick = r * 1000, SteamId = "P1" }).ToList(),
        Rounds = allRoundNumbers.Select(r => new Round
            { RoundNumber = r, TickStart = r * 1000, TickEnd = r * 1000 + 9000 }).ToList()
    };

    [Test]
    public async Task Three_consecutive_deaths_detected()
    {
        var match = BuildMatch([1, 2, 3], [1, 2, 3, 4, 5]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LowlightType, Is.EqualTo(LowlightType.DeathStreak));
        Assert.That(result[0].Description, Does.Contain("R1–R3"));
    }

    [Test]
    public async Task Two_consecutive_deaths_below_threshold_not_detected()
    {
        var match = BuildMatch([1, 2], [1, 2, 3, 4, 5]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Gap_in_deaths_resets_streak()
    {
        // died in 1,2, survived 3, died in 4,5 — no streak of 3
        var match = BuildMatch([1, 2, 4, 5], [1, 2, 3, 4, 5]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Two_separate_streaks_produce_two_highlights()
    {
        var match = BuildMatch([1, 2, 3, 5, 6, 7], Enumerable.Range(1, 8));
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(h => h.RoundNumber), Is.EquivalentTo(new[] { 1, 5 }));
    }

    [Test]
    public async Task Streak_longer_than_threshold_produces_single_highlight()
    {
        // 5-death streak should produce exactly one lowlight entry
        var match = BuildMatch([1, 2, 3, 4, 5], Enumerable.Range(1, 6));
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Description, Does.Contain("5 consecutive"));
    }

    [Test]
    public async Task Disabled_option_returns_empty()
    {
        _options.DeathStreakEnabled = false;
        var match = BuildMatch([1, 2, 3], Enumerable.Range(1, 5));
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task No_deaths_returns_empty()
    {
        var match = BuildMatch([], Enumerable.Range(1, 5));
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }
}
