using CS2Highlights.Database.Entities;

namespace CS2Highlights.Database;

public class SettingsRepository
{
    private readonly AppDbContext _db;

    public SettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public string? Get(string key)
    {
        return _db.UserSettings.Find(key)?.Value;
    }

    public void Set(string key, string value)
    {
        var existing = _db.UserSettings.Find(key);
        if (existing is null)
            _db.UserSettings.Add(new UserSettingEntity { Key = key, Value = value });
        else
            existing.Value = value;

        _db.SaveChanges();
    }
}
