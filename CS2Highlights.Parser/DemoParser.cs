using System.Security.Cryptography;
using CS2Highlights.Core.Enums;
using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.Database.Entities;
using DemoFile;
using DemoFile.Game.Cs;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.Parser;

public class DemoParser : IDemoParser
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public DemoParser(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<PlayerInfo>> ReadPlayersAsync(string demoPath)
    {
        var seenPlayers = new Dictionary<ulong, PlayerInfo>();
        using var linkedCts = new CancellationTokenSource();
        await using var stream = File.OpenRead(demoPath);
        var demo = new CsDemoParser();

        demo.EntityEvents.CCSPlayerController.Create += c =>
        {
            if (c.SteamID > 0)
                seenPlayers[c.SteamID] = new PlayerInfo
                {
                    SteamId = c.SteamID.ToString(),
                    PlayerName = c.PlayerName ?? string.Empty,
                    Team = c.CSTeamNum.ToString()
                };
        };

        demo.OnCommandFinish += () =>
        {
            if (demo.CurrentDemoTick.Value > 0 && seenPlayers.Count >= 10)
                linkedCts.Cancel();
        };

        var reader = DemoFileReader.Create(demo, stream);
        try { await reader.ReadAllAsync(linkedCts.Token); }
        catch (OperationCanceledException) { }

        return seenPlayers.Values.ToList();
    }

    public async Task<ParsedMatch> ParseAsync(string demoPath, PlayerInfo selectedPlayer)
    {
        var matchId = ComputeMatchId(demoPath);

        using var db = _factory.CreateDbContext();
        var existing = db.Matches
            .Include(m => m.Rounds)
            .Include(m => m.KillEvents)
            .Include(m => m.GrenadeEvents)
            .FirstOrDefault(m => m.MatchId == matchId && m.SelectedPlayerSteamId == selectedPlayer.SteamId);

        if (existing != null)
            return BuildFromDb(existing);

        var parsed = await FullParseAsync(demoPath, selectedPlayer, matchId);
        await SaveToDbAsync(db, parsed, demoPath);
        return parsed;
    }

    private static async Task<ParsedMatch> FullParseAsync(string demoPath, PlayerInfo selectedPlayer, string matchId)
    {
        var allPlayers = new Dictionary<ulong, PlayerInfo>();
        var rounds = new List<Round>();
        var kills = new List<KillEvent>();
        var deaths = new List<DeathEvent>();
        var grenades = new List<GrenadeEvent>();
        string mapName = string.Empty;

        int currentRound = 0;
        int roundStartTick = 0;

        GrenadeEvent? pendingInstantGrenade = null;  // HE or Flash — resolved within one tick
        var pendingFires = new List<GrenadeEvent>();  // Molotov/Incendiary — spans ticks until round end

        var demo = new CsDemoParser();

        demo.EntityEvents.CCSPlayerController.Create += c =>
        {
            if (c.SteamID > 0)
                allPlayers[c.SteamID] = new PlayerInfo
                {
                    SteamId = c.SteamID.ToString(),
                    PlayerName = c.PlayerName ?? string.Empty,
                    Team = c.CSTeamNum.ToString()
                };
        };

        demo.Source1GameEvents.RoundStart += _ =>
        {
            currentRound++;
            roundStartTick = demo.CurrentDemoTick.Value;
            grenades.AddRange(pendingFires);
            pendingFires.Clear();
        };

        demo.Source1GameEvents.RoundEnd += e =>
        {
            if (currentRound <= 0) return;
            var winner = e.Winner == 3 ? TeamSide.CT : TeamSide.T;
            rounds.Add(new Round
            {
                RoundNumber = currentRound,
                TickStart = roundStartTick,
                TickEnd = demo.CurrentDemoTick.Value,
                WinnerSide = winner
            });
            grenades.AddRange(pendingFires);
            pendingFires.Clear();
        };

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            if (currentRound <= 0) return;
            var attackerSteamId = e.Attacker?.SteamID.ToString() ?? string.Empty;
            var victimSteamId = e.Player?.SteamID.ToString() ?? string.Empty;
            var tick = demo.CurrentDemoTick.Value;

            kills.Add(new KillEvent
            {
                Tick = tick,
                RoundNumber = currentRound,
                SteamId = attackerSteamId,
                VictimSteamId = victimSteamId,
                Weapon = e.Weapon ?? string.Empty,
                IsHeadshot = e.Headshot,
                IsWallbang = e.Penetrated > 0,
                IsNoscope = e.Noscope
            });

            if (victimSteamId == selectedPlayer.SteamId)
                deaths.Add(new DeathEvent
                {
                    Tick = tick,
                    RoundNumber = currentRound,
                    SteamId = victimSteamId,
                    KillerSteamId = attackerSteamId,
                    TimeIntoRound = 0f
                });
        };

        demo.Source1GameEvents.HegrenadeDetonate += e =>
        {
            if (currentRound <= 0 || e.Player?.SteamID.ToString() != selectedPlayer.SteamId) return;
            pendingInstantGrenade = new GrenadeEvent
            {
                Tick = demo.CurrentDemoTick.Value,
                RoundNumber = currentRound,
                SteamId = selectedPlayer.SteamId,
                GrenadeType = GrenadeType.HE
            };
        };

        demo.Source1GameEvents.FlashbangDetonate += e =>
        {
            if (currentRound <= 0 || e.Player?.SteamID.ToString() != selectedPlayer.SteamId) return;
            pendingInstantGrenade = new GrenadeEvent
            {
                Tick = demo.CurrentDemoTick.Value,
                RoundNumber = currentRound,
                SteamId = selectedPlayer.SteamId,
                GrenadeType = GrenadeType.Flash
            };
        };

        demo.Source1GameEvents.MolotovDetonate += e =>
        {
            if (currentRound <= 0 || e.Player?.SteamID.ToString() != selectedPlayer.SteamId) return;
            var type = e.Player.CSTeamNum == CSTeamNumber.CounterTerrorist
                ? GrenadeType.Incendiary
                : GrenadeType.Molotov;
            pendingFires.Add(new GrenadeEvent
            {
                Tick = demo.CurrentDemoTick.Value,
                RoundNumber = currentRound,
                SteamId = selectedPlayer.SteamId,
                GrenadeType = type
            });
        };

        demo.Source1GameEvents.SmokegrenadeDetonate += e =>
        {
            if (currentRound <= 0 || e.Player?.SteamID.ToString() != selectedPlayer.SteamId) return;
            grenades.Add(new GrenadeEvent
            {
                Tick = demo.CurrentDemoTick.Value,
                RoundNumber = currentRound,
                SteamId = selectedPlayer.SteamId,
                GrenadeType = GrenadeType.Smoke
            });
        };

        demo.Source1GameEvents.DecoyDetonate += e =>
        {
            if (currentRound <= 0 || e.Player?.SteamID.ToString() != selectedPlayer.SteamId) return;
            grenades.Add(new GrenadeEvent
            {
                Tick = demo.CurrentDemoTick.Value,
                RoundNumber = currentRound,
                SteamId = selectedPlayer.SteamId,
                GrenadeType = GrenadeType.Decoy
            });
        };

        demo.Source1GameEvents.PlayerHurt += e =>
        {
            if (e.Attacker?.SteamID.ToString() != selectedPlayer.SteamId) return;
            var weapon = e.Weapon ?? string.Empty;
            bool isEnemy = e.Player?.CSTeamNum != e.Attacker?.CSTeamNum;

            if (pendingInstantGrenade?.GrenadeType == GrenadeType.HE && weapon == "hegrenade")
            {
                if (isEnemy) pendingInstantGrenade.DamageToEnemies += e.DmgHealth;
                else pendingInstantGrenade.DamageToTeam += e.DmgHealth;
            }
            else if (weapon == "inferno")
            {
                var latestFire = pendingFires.LastOrDefault(f => f.RoundNumber == currentRound);
                if (latestFire != null)
                {
                    if (isEnemy) latestFire.DamageToEnemies += e.DmgHealth;
                    else latestFire.DamageToTeam += e.DmgHealth;
                }
            }
        };

        demo.Source1GameEvents.PlayerBlind += e =>
        {
            if (pendingInstantGrenade?.GrenadeType != GrenadeType.Flash) return;
            if (e.Attacker?.SteamID.ToString() != selectedPlayer.SteamId) return;
            if (e.BlindDuration < 0.5f) return;

            bool isEnemy = e.Player?.CSTeamNum != e.Attacker?.CSTeamNum;
            if (isEnemy) pendingInstantGrenade.EnemiesBlinded++;
            else pendingInstantGrenade.TeammatesBlinded++;
        };

        // Fires after every command — finalizes HE/Flash grenade after same-tick damage events
        demo.OnCommandFinish += () =>
        {
            if (demo.FileHeader is { } h) mapName = h.MapName ?? string.Empty;

            if (pendingInstantGrenade != null)
            {
                grenades.Add(pendingInstantGrenade);
                pendingInstantGrenade = null;
            }
        };

        await using var stream = File.OpenRead(demoPath);
        var reader = DemoFileReader.Create(demo, stream);
        await reader.ReadAllAsync();

        // Finalize any fires that didn't get closed by a round_end (e.g. end of demo)
        grenades.AddRange(pendingFires);

        return new ParsedMatch
        {
            MatchId = matchId,
            DemoPath = demoPath,
            Map = mapName,
            Date = File.GetLastWriteTime(demoPath),
            SelectedPlayer = selectedPlayer,
            AllPlayers = allPlayers.Values.ToList(),
            Rounds = rounds,
            Kills = kills,
            Deaths = deaths,
            Grenades = grenades
        };
    }

    private static async Task SaveToDbAsync(AppDbContext db, ParsedMatch parsed, string demoPath)
    {
        var match = new MatchEntity
        {
            MatchId = parsed.MatchId,
            DemoPath = parsed.DemoPath,
            DemoFileName = Path.GetFileName(demoPath),
            Map = parsed.Map,
            Date = parsed.Date,
            SelectedPlayerSteamId = parsed.SelectedPlayer.SteamId,
            SelectedPlayerName = parsed.SelectedPlayer.PlayerName,
            ParsedAt = DateTime.Now
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        var roundEntities = parsed.Rounds.Select(r => new RoundEntity
        {
            MatchId = match.Id,
            RoundNumber = r.RoundNumber,
            TickStart = r.TickStart,
            TickEnd = r.TickEnd,
            WinnerSide = r.WinnerSide
        }).ToList();
        db.Rounds.AddRange(roundEntities);
        await db.SaveChangesAsync();

        var roundLookup = roundEntities.ToDictionary(r => r.RoundNumber, r => r.Id);

        db.KillEvents.AddRange(parsed.Kills.Select(k => new KillEventEntity
        {
            MatchId = match.Id,
            RoundId = roundLookup.GetValueOrDefault(k.RoundNumber),
            Tick = k.Tick,
            KillerSteamId = k.SteamId,
            VictimSteamId = k.VictimSteamId,
            Weapon = k.Weapon,
            IsHeadshot = k.IsHeadshot,
            IsWallbang = k.IsWallbang,
            IsNoscope = k.IsNoscope
        }));

        db.GrenadeEvents.AddRange(parsed.Grenades.Select(g => new GrenadeEventEntity
        {
            MatchId = match.Id,
            RoundId = roundLookup.GetValueOrDefault(g.RoundNumber),
            Tick = g.Tick,
            ThrowerSteamId = g.SteamId,
            GrenadeType = g.GrenadeType,
            DmgToEnemies = g.DamageToEnemies,
            DmgToTeam = g.DamageToTeam,
            EnemiesBlinded = g.EnemiesBlinded,
            TeammatesBlinded = g.TeammatesBlinded
        }));

        await db.SaveChangesAsync();
    }

    private static ParsedMatch BuildFromDb(MatchEntity match)
    {
        var roundById = match.Rounds.ToDictionary(r => r.Id, r => r.RoundNumber);

        return new ParsedMatch
        {
            MatchId = match.MatchId,
            DemoPath = match.DemoPath,
            Map = match.Map,
            Date = match.Date,
            SelectedPlayer = new PlayerInfo
            {
                SteamId = match.SelectedPlayerSteamId,
                PlayerName = match.SelectedPlayerName
            },
            AllPlayers = [],
            Rounds = match.Rounds.Select(r => new Round
            {
                RoundNumber = r.RoundNumber,
                TickStart = r.TickStart,
                TickEnd = r.TickEnd,
                WinnerSide = r.WinnerSide
            }).ToList(),
            Kills = match.KillEvents.Select(k => new KillEvent
            {
                Tick = k.Tick,
                RoundNumber = roundById.GetValueOrDefault(k.RoundId),
                SteamId = k.KillerSteamId,
                VictimSteamId = k.VictimSteamId,
                Weapon = k.Weapon,
                IsHeadshot = k.IsHeadshot,
                IsWallbang = k.IsWallbang,
                IsNoscope = k.IsNoscope
            }).ToList(),
            Deaths = [],
            Grenades = match.GrenadeEvents.Select(g => new GrenadeEvent
            {
                Tick = g.Tick,
                RoundNumber = roundById.GetValueOrDefault(g.RoundId),
                SteamId = g.ThrowerSteamId,
                GrenadeType = g.GrenadeType,
                DamageToEnemies = g.DmgToEnemies,
                DamageToTeam = g.DmgToTeam,
                EnemiesBlinded = g.EnemiesBlinded,
                TeammatesBlinded = g.TeammatesBlinded
            }).ToList()
        };
    }

    private static string ComputeMatchId(string demoPath)
    {
        using var stream = File.OpenRead(demoPath);
        var buffer = new byte[65536];
        var read = stream.Read(buffer, 0, buffer.Length);
        var hash = SHA256.HashData(buffer.AsSpan(0, read));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
