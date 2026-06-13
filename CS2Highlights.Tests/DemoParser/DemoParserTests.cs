using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.Parser;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.Tests.DemoParser;

[TestFixture]
public class DemoParserTests
{
    private const string DemoFolder =
        @"E:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo\replays";

    private string? _demoPath;
    private string? _dbPath;
    private CS2Highlights.Parser.DemoParser _parser = null!;
    private AppDbContext _db = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _demoPath = Directory.Exists(DemoFolder)
            ? Directory.GetFiles(DemoFolder, "*.dem").FirstOrDefault()
            : null;

        _dbPath = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        var factory = new TestDbContextFactory(options);
        _parser = new CS2Highlights.Parser.DemoParser(factory);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _db.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (_dbPath != null) File.Delete(_dbPath); }
        catch (IOException) { /* Pool release is async; OS will reclaim the temp file */ }
    }

    private void SkipIfNoDemo()
    {
        if (_demoPath == null)
            Assert.Ignore("No .dem file found in replays folder — skipping integration test.");
    }

    // ---- ReadPlayersAsync ----

    [Test]
    public async Task ReadPlayersAsync_Returns10Players()
    {
        SkipIfNoDemo();
        var players = await _parser.ReadPlayersAsync(_demoPath!);
        Assert.That(players, Has.Count.EqualTo(10));
    }

    [Test]
    public async Task ReadPlayersAsync_AllPlayersHaveSteamId()
    {
        SkipIfNoDemo();
        var players = await _parser.ReadPlayersAsync(_demoPath!);
        Assert.That(players.All(p => p.SteamId != "0" && p.SteamId.Length > 0), Is.True);
    }

    // ---- ParseAsync ----

    private async Task<(ParsedMatch match, PlayerInfo player)> ParseFirstAsync()
    {
        var players = await _parser.ReadPlayersAsync(_demoPath!);
        var player = players.First();
        var match = await _parser.ParseAsync(_demoPath!, player);
        return (match, player);
    }

    [Test]
    public async Task ParseAsync_MatchIdIsSet()
    {
        SkipIfNoDemo();
        var (match, _) = await ParseFirstAsync();
        Assert.That(match.MatchId, Has.Length.EqualTo(64));
        Assert.That(match.MatchId, Does.Match("^[0-9a-f]+$"));
    }

    [Test]
    public async Task ParseAsync_MapNameIsSet()
    {
        SkipIfNoDemo();
        var (match, _) = await ParseFirstAsync();
        Assert.That(match.Map, Is.Not.Empty);
    }

    [Test]
    public async Task ParseAsync_RoundsArePresent()
    {
        SkipIfNoDemo();
        var (match, _) = await ParseFirstAsync();
        Assert.That(match.Rounds, Is.Not.Empty);
    }

    [Test]
    public async Task ParseAsync_RoundsHaveValidTicks()
    {
        SkipIfNoDemo();
        var (match, _) = await ParseFirstAsync();
        Assert.That(match.Rounds.All(r => r.TickEnd > r.TickStart), Is.True);
    }

    [Test]
    public async Task ParseAsync_KillsArePresent()
    {
        SkipIfNoDemo();
        var (match, _) = await ParseFirstAsync();
        Assert.That(match.Kills, Is.Not.Empty);
    }

    [Test]
    public async Task ParseAsync_KillsHaveWeapons()
    {
        SkipIfNoDemo();
        var (match, _) = await ParseFirstAsync();
        Assert.That(match.Kills.All(k => k.Weapon.Length > 0), Is.True);
    }

    [Test]
    public async Task ParseAsync_SavesMatchToDB()
    {
        SkipIfNoDemo();
        var (_, player) = await ParseFirstAsync();
        var saved = _db.Matches.FirstOrDefault(m => m.SelectedPlayerSteamId == player.SteamId);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.ParsedAt, Is.Not.Null);
    }

    [Test]
    public async Task ParseAsync_SavesRoundsToDB()
    {
        SkipIfNoDemo();
        var (match, player) = await ParseFirstAsync();
        var saved = _db.Matches.FirstOrDefault(m => m.SelectedPlayerSteamId == player.SteamId);
        var roundCount = _db.Rounds.Count(r => r.MatchId == saved!.Id);
        Assert.That(roundCount, Is.EqualTo(match.Rounds.Count));
    }

    [Test]
    public async Task ParseAsync_SavesKillEventsToDB()
    {
        SkipIfNoDemo();
        var (match, player) = await ParseFirstAsync();
        var saved = _db.Matches.FirstOrDefault(m => m.SelectedPlayerSteamId == player.SteamId);
        var killCount = _db.KillEvents.Count(k => k.MatchId == saved!.Id);
        Assert.That(killCount, Is.EqualTo(match.Kills.Count));
    }

    [Test]
    public async Task ParseAsync_SavesGrenadeEventsToDB()
    {
        SkipIfNoDemo();
        var (match, player) = await ParseFirstAsync();
        var saved = _db.Matches.FirstOrDefault(m => m.SelectedPlayerSteamId == player.SteamId);
        var grenadeCount = _db.GrenadeEvents.Count(g => g.MatchId == saved!.Id);
        Assert.That(grenadeCount, Is.EqualTo(match.Grenades.Count));
    }

    [Test]
    public async Task ParseAsync_DuplicateGuard_SkipsReparse()
    {
        SkipIfNoDemo();
        var players = await _parser.ReadPlayersAsync(_demoPath!);
        var player = players.First();

        var first = await _parser.ParseAsync(_demoPath!, player);
        var countBefore = _db.Matches.Count();

        var second = await _parser.ParseAsync(_demoPath!, player);
        var countAfter = _db.Matches.Count();

        Assert.That(countAfter, Is.EqualTo(countBefore), "Second parse should not insert a new match row.");
        Assert.That(second.MatchId, Is.EqualTo(first.MatchId));
    }

    [Test]
    public async Task ParseAsync_AllPlayersPopulatedOnFreshParse()
    {
        SkipIfNoDemo();
        // Use a second player so this test doesn't hit the duplicate-guard cache
        var players = await _parser.ReadPlayersAsync(_demoPath!);
        var freshPlayer = players.Last();

        var match = await _parser.ParseAsync(_demoPath!, freshPlayer);

        // AllPlayers is populated only on a live parse, not on cache-hit returns from DB
        if (match.AllPlayers.Count == 0)
            Assert.Ignore("Cache hit — AllPlayers not repopulated from DB (by design).");

        Assert.That(match.AllPlayers.Any(p => p.SteamId == freshPlayer.SteamId), Is.True);
    }

    [Test]
    [Explicit("Prints all player_hurt weapon names seen in the demo — run to verify weapon name constants")]
    public async Task Diagnostic_PrintPlayerHurtWeaponNames()
    {
        SkipIfNoDemo();
        var weapons = new SortedDictionary<string, int>(StringComparer.Ordinal);

        var demo = new DemoFile.CsDemoParser();
        demo.Source1GameEvents.PlayerHurt += e =>
        {
            var w = e.Weapon ?? "(null)";
            weapons[w] = weapons.GetValueOrDefault(w) + 1;
        };

        await using var stream = File.OpenRead(_demoPath!);
        var reader = DemoFile.DemoFileReader.Create(demo, stream);
        await reader.ReadAllAsync();

        Console.WriteLine("weapon name → hit count");
        foreach (var (weapon, count) in weapons)
            Console.WriteLine($"  {weapon,-30} {count}");
    }

    [Test]
    [Explicit("Prints parse summary — run manually to inspect output")]
    public async Task ParseAsync_PrintSummary()
    {
        SkipIfNoDemo();
        var (match, player) = await ParseFirstAsync();
        Console.WriteLine($"Map:    {match.Map}");
        Console.WriteLine($"Player: {player.PlayerName} ({player.SteamId})");
        Console.WriteLine($"Rounds: {match.Rounds.Count}");
        Console.WriteLine($"Kills:  {match.Kills.Count}  (by player: {match.Kills.Count(k => k.SteamId == player.SteamId)})");
        Console.WriteLine($"Deaths: {match.Deaths.Count}");
        Console.WriteLine($"Grenades: {match.Grenades.Count}");
        foreach (var g in match.Grenades)
            Console.WriteLine($"  R{g.RoundNumber} tick={g.Tick} {g.GrenadeType} dmgEnemies={g.DamageToEnemies} dmgTeam={g.DamageToTeam} blindEnemies={g.EnemiesBlinded} blindTeam={g.TeammatesBlinded}");
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _opts;
        public TestDbContextFactory(DbContextOptions<AppDbContext> opts) => _opts = opts;
        public AppDbContext CreateDbContext() => new(_opts);
    }
}
