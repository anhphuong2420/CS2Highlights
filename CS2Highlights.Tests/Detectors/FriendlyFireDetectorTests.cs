using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class FriendlyFireDetectorTests
{
    private FriendlyFireDetector _detector = null!;
    private DetectionOptions _options = null!;
    private PlayerInfo _player = null!;

    [SetUp]
    public void SetUp()
    {
        _detector = new FriendlyFireDetector();
        _options = new DetectionOptions { FriendlyFireEnabled = true, FriendlyFireDamageThreshold = 40 };
        _player = new PlayerInfo { SteamId = "P1", PlayerName = "TestPlayer" };
    }

    private GrenadeEvent HETeamDmg(int round, int tick, int teamDmg) => new()
    {
        RoundNumber = round, Tick = tick, SteamId = "P1",
        GrenadeType = GrenadeType.HE, DamageToTeam = teamDmg, DamageToEnemies = 0
    };

    private ParsedMatch BuildMatch(List<GrenadeEvent> grenades) => new()
    {
        MatchId = "test", SelectedPlayer = _player, Grenades = grenades
    };

    [Test]
    public async Task Over_threshold_produces_FriendlyFire()
    {
        var match = BuildMatch([HETeamDmg(1, 500, 50)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LowlightType, Is.EqualTo(LowlightType.FriendlyFire));
    }

    [Test]
    public async Task Exactly_at_threshold_detected()
    {
        var match = BuildMatch([HETeamDmg(1, 500, 40)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Below_threshold_not_detected()
    {
        var match = BuildMatch([HETeamDmg(1, 500, 20)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Multiple_grenades_same_round_damage_aggregated()
    {
        // Two grenades: 25 + 25 = 50 >= threshold 40
        var match = BuildMatch([HETeamDmg(1, 500, 25), HETeamDmg(1, 700, 25)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Description, Does.Contain("50"));
    }

    [Test]
    public async Task Different_rounds_each_evaluated_separately()
    {
        // R1: 50 >= 40 → detect; R2: 20 < 40 → skip
        var match = BuildMatch([HETeamDmg(1, 500, 50), HETeamDmg(2, 1500, 20)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].RoundNumber, Is.EqualTo(1));
    }

    [Test]
    public async Task Zero_team_damage_grenade_ignored()
    {
        var match = BuildMatch([HETeamDmg(1, 500, 0)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Disabled_returns_empty()
    {
        _options.FriendlyFireEnabled = false;
        var match = BuildMatch([HETeamDmg(1, 500, 100)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }
}
