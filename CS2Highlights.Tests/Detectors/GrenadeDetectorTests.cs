using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Models;
using CS2Highlights.Parser;

namespace CS2Highlights.Tests.Detectors;

[TestFixture]
public class GrenadeDetectorTests
{
    private GrenadeDetector _detector = null!;
    private DetectionOptions _options = null!;
    private PlayerInfo _player = null!;

    [SetUp]
    public void SetUp()
    {
        _detector = new GrenadeDetector();
        _options = new DetectionOptions
        {
            TeamFlashEnabled             = true, TeamFlashMinTeammatesBlinded = 2,
            TeamMolotovEnabled           = true,
            WastedGrenadeEnabled         = true,
            LowDamageGrenadeEnabled      = false, LowDamageGrenadeThreshold = 20
        };
        _player = new PlayerInfo { SteamId = "P1", PlayerName = "TestPlayer" };
    }

    private ParsedMatch BuildMatch(List<GrenadeEvent> grenades) => new()
    {
        MatchId = "test", SelectedPlayer = _player, Grenades = grenades
    };

    private GrenadeEvent G(GrenadeType type, int round = 1, int tick = 500,
        int teammatesBlinded = 0, int enemiesBlinded = 0, int dmgTeam = 0, int dmgEnemy = 0) => new()
    {
        GrenadeType = type, RoundNumber = round, Tick = tick, SteamId = "P1",
        TeammatesBlinded = teammatesBlinded, EnemiesBlinded = enemiesBlinded,
        DamageToTeam = dmgTeam, DamageToEnemies = dmgEnemy
    };

    // ---- TeamFlash ----

    [Test]
    public async Task TeamFlash_enough_teammates_blinded_detected()
    {
        var match = BuildMatch([G(GrenadeType.Flash, teammatesBlinded: 2)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LowlightType, Is.EqualTo(LowlightType.TeamFlash));
    }

    [Test]
    public async Task TeamFlash_not_enough_teammates_not_detected()
    {
        var match = BuildMatch([G(GrenadeType.Flash, teammatesBlinded: 1)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task TeamFlash_disabled_not_detected()
    {
        _options.TeamFlashEnabled = false;
        var match = BuildMatch([G(GrenadeType.Flash, teammatesBlinded: 3)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- TeamMolotov ----

    [Test]
    public async Task TeamMolotov_with_team_damage_detected()
    {
        var match = BuildMatch([G(GrenadeType.Molotov, dmgTeam: 30)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LowlightType, Is.EqualTo(LowlightType.TeamMolotov));
    }

    [Test]
    public async Task TeamMolotov_incendiary_also_detected()
    {
        var match = BuildMatch([G(GrenadeType.Incendiary, dmgTeam: 15)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task TeamMolotov_zero_team_damage_not_detected()
    {
        var match = BuildMatch([G(GrenadeType.Molotov, dmgTeam: 0)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task TeamMolotov_disabled_not_detected()
    {
        _options.TeamMolotovEnabled = false;
        var match = BuildMatch([G(GrenadeType.Molotov, dmgTeam: 50)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- WastedGrenade ----

    [Test]
    public async Task WastedGrenade_zero_enemy_damage_detected()
    {
        var match = BuildMatch([G(GrenadeType.HE, dmgEnemy: 0)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LowlightType, Is.EqualTo(LowlightType.WastedGrenade));
    }

    [Test]
    public async Task WastedGrenade_has_enemy_damage_not_detected()
    {
        var match = BuildMatch([G(GrenadeType.HE, dmgEnemy: 50)]);
        var result = await _detector.DetectAsync(match, _options);
        // LowDamageGrenadeEnabled = false, so no lowlight from damage either
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task WastedGrenade_disabled_not_detected()
    {
        _options.WastedGrenadeEnabled = false;
        var match = BuildMatch([G(GrenadeType.HE, dmgEnemy: 0)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result, Is.Empty);
    }

    // ---- LowDamageGrenade ----

    [Test]
    public async Task LowDamageGrenade_enabled_detects_below_threshold()
    {
        _options.LowDamageGrenadeEnabled = true;
        _options.LowDamageGrenadeThreshold = 20;
        var match = BuildMatch([G(GrenadeType.HE, dmgEnemy: 10)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result.Any(h => h.LowlightType == LowlightType.LowDamageGrenade), Is.True);
    }

    [Test]
    public async Task LowDamageGrenade_disabled_by_default_not_detected()
    {
        // Default DetectionOptions has LowDamageGrenadeEnabled = false
        var match = BuildMatch([G(GrenadeType.HE, dmgEnemy: 5)]);
        var result = await _detector.DetectAsync(match, _options);
        Assert.That(result.Any(h => h.LowlightType == LowlightType.LowDamageGrenade), Is.False);
    }
}
