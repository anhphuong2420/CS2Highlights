using CS2Highlights.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.Database;

public class SettingsRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SettingsRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public string? Get(string key)
    {
        using var db = _factory.CreateDbContext();
        return db.UserSettings.Find(key)?.Value;
    }

    public void Set(string key, string value)
    {
        using var db = _factory.CreateDbContext();
        var existing = db.UserSettings.Find(key);
        if (existing is null)
            db.UserSettings.Add(new UserSettingEntity { Key = key, Value = value });
        else
            existing.Value = value;
        db.SaveChanges();
    }
}
