using CS2Highlights.DemoScanner;

namespace CS2Highlights.Tests.DemoScanner;

/// <summary>
/// Integration tests — require a real .dem file at the path below.
/// </summary>
[TestFixture]
public class LightweightDemoReaderTests
{
    private const string DemoFolder = @"E:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo\replays";

    private LightweightDemoReader _reader = null!;
    private string _demoPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _reader = new LightweightDemoReader();
        _demoPath = Directory.GetFiles(DemoFolder, "*.dem").FirstOrDefault() ?? string.Empty;
    }

    private void SkipIfNoDemoFile()
    {
        if (!File.Exists(_demoPath))
            Assert.Ignore($"No .dem file found in {DemoFolder} — skipping integration test.");
    }

    [Test]
    public async Task ReadHeaderAsync_ReturnsMatchId()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(result.MatchId, Is.Not.Empty);
        Assert.That(result.MatchId, Has.Length.EqualTo(64)); // SHA-256 hex = 64 chars
    }

    [Test]
    public async Task ReadHeaderAsync_MatchIdIsStable()
    {
        SkipIfNoDemoFile();

        var first = await _reader.ReadHeaderAsync(_demoPath);
        var second = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(first.MatchId, Is.EqualTo(second.MatchId));
    }

    [Test]
    public async Task ReadHeaderAsync_ReturnsMapName()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(result.MapName, Is.Not.Empty);
        Console.WriteLine($"Map: {result.MapName}");
    }

    [Test]
    public async Task ReadHeaderAsync_ReturnsMatchDate()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(result.MatchDate, Is.Not.EqualTo(default(DateTime)));
        Console.WriteLine($"Match date: {result.MatchDate}");
    }

    [Test]
    public async Task ReadHeaderAsync_ReturnsTenPlayers()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(result.Players, Has.Count.EqualTo(10));
    }

    [Test]
    public async Task ReadHeaderAsync_AllPlayersHaveSteamId()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(result.Players, Is.All.Matches<CS2Highlights.Core.Models.PlayerInfo>(
            p => !string.IsNullOrEmpty(p.SteamId) && p.SteamId != "0"
        ));
    }

    [Test]
    public async Task ReadHeaderAsync_AllPlayersHaveName()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Assert.That(result.Players, Is.All.Matches<CS2Highlights.Core.Models.PlayerInfo>(
            p => !string.IsNullOrWhiteSpace(p.PlayerName)
        ));
    }

    [Test]
    public async Task ReadHeaderAsync_PrintsAllPlayers()
    {
        SkipIfNoDemoFile();

        var result = await _reader.ReadHeaderAsync(_demoPath);

        Console.WriteLine($"Map: {result.MapName}  |  Match: {result.MatchDate}");
        Console.WriteLine($"Match ID: {result.MatchId}");
        Console.WriteLine("Players:");
        foreach (var p in result.Players)
            Console.WriteLine($"  [{p.Team,-22}] {p.PlayerName,-24} {p.SteamId}");

        Assert.That(result.Players, Is.Not.Empty);
    }
}
