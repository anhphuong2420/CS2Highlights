using CS2Highlights.Core.Enums;
using CS2Highlights.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CS2Highlights.Database;

public class AppDbContext : DbContext
{
    public DbSet<MatchEntity> Matches { get; set; }
    public DbSet<RoundEntity> Rounds { get; set; }
    public DbSet<KillEventEntity> KillEvents { get; set; }
    public DbSet<GrenadeEventEntity> GrenadeEvents { get; set; }
    public DbSet<HighlightEntity> Highlights { get; set; }
    public DbSet<RenderJobEntity> RenderJobs { get; set; }
    public DbSet<UserSettingEntity> UserSettings { get; set; }
    public DbSet<DemoDetailEntity> DemoDetails { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchEntity>()
            .HasIndex(m => new { m.MatchId, m.SelectedPlayerSteamId })
            .IsUnique();

        modelBuilder.Entity<RoundEntity>()
            .Property(r => r.WinnerSide)
            .HasConversion<string>();

        modelBuilder.Entity<GrenadeEventEntity>()
            .Property(g => g.GrenadeType)
            .HasConversion<string>();

        modelBuilder.Entity<HighlightEntity>()
            .Property(h => h.HighlightType)
            .HasConversion<string>();

        modelBuilder.Entity<HighlightEntity>()
            .Property(h => h.LowlightType)
            .HasConversion<string>();

        modelBuilder.Entity<HighlightEntity>()
            .Property(h => h.RenderStatus)
            .HasConversion<string>();

        modelBuilder.Entity<RenderJobEntity>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<DemoDetailEntity>()
            .HasIndex(d => d.FileName)
            .IsUnique();
    }
}
