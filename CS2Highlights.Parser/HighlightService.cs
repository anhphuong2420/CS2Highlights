using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;
using CS2Highlights.Database;
using CS2Highlights.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.Parser;

public class HighlightService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IEnumerable<IHighlightDetector> _detectors;

    public HighlightService(IDbContextFactory<AppDbContext> factory, IEnumerable<IHighlightDetector> detectors)
    {
        _factory = factory;
        _detectors = detectors;
    }

    public async Task<List<Highlight>> RunAsync(ParsedMatch match, DetectionOptions options)
    {
        using var db = _factory.CreateDbContext();

        var matchEntity = db.Matches.FirstOrDefault(m =>
            m.MatchId == match.MatchId &&
            m.SelectedPlayerSteamId == match.SelectedPlayer.SteamId);

        if (matchEntity == null) return [];

        // Return cached highlights if detection already ran
        var existing = db.Highlights.Where(h => h.MatchId == matchEntity.Id).ToList();
        if (existing.Count > 0)
            return MapToModel(existing, matchEntity, db);

        // Run detectors
        var allHighlights = new List<Highlight>();
        foreach (var detector in _detectors)
        {
            var found = await detector.DetectAsync(match, options);
            allHighlights.AddRange(found);
        }

        // Save
        var roundLookup = db.Rounds
            .Where(r => r.MatchId == matchEntity.Id)
            .ToDictionary(r => r.RoundNumber, r => r.Id);

        db.Highlights.AddRange(allHighlights.Select(h => new HighlightEntity
        {
            MatchId = matchEntity.Id,
            RoundId = roundLookup.GetValueOrDefault(h.RoundNumber),
            HighlightType = h.HighlightType,
            LowlightType = h.LowlightType,
            TickStart = h.TickStart,
            TickEnd = h.TickEnd,
            Description = h.Description,
            RenderStatus = Core.Enums.RenderStatus.Queued
        }));

        await db.SaveChangesAsync();

        return allHighlights;
    }

    private static List<Highlight> MapToModel(
        List<HighlightEntity> entities, MatchEntity matchEntity, AppDbContext db)
    {
        var roundNumberById = db.Rounds
            .Where(r => r.MatchId == matchEntity.Id)
            .ToDictionary(r => r.Id, r => r.RoundNumber);

        return entities.Select(h => new Highlight
        {
            HighlightId = h.Id,
            MatchId = matchEntity.MatchId,
            RoundNumber = h.RoundId.HasValue && roundNumberById.TryGetValue(h.RoundId.Value, out var rn) ? rn : 0,
            HighlightType = h.HighlightType,
            LowlightType = h.LowlightType,
            TickStart = h.TickStart,
            TickEnd = h.TickEnd,
            Description = h.Description,
            ClipPath = h.ClipPath,
            RenderStatus = h.RenderStatus
        }).ToList();
    }
}
