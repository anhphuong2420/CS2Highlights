namespace CS2Highlights.Core.Models;

public class DetectionOptions
{
    // Highlights
    public bool MultiKillEnabled { get; set; } = true;
    public int MultiKillMinKills { get; set; } = 3;

    public bool ClutchEnabled { get; set; } = true;

    public bool EntryFragEnabled { get; set; } = true;
    public int EntryFragTimeSeconds { get; set; } = 8;

    public bool WallbangEnabled { get; set; } = true;

    public bool HeadshotStreakEnabled { get; set; } = true;
    public int HeadshotStreakCount { get; set; } = 3;

    public bool OutnumberedWinEnabled { get; set; } = true;
    public int OutnumberedWinMinEnemies { get; set; } = 2;

    // Lowlights
    public bool DeathStreakEnabled { get; set; } = true;
    public int DeathStreakCount { get; set; } = 3;

    public bool FriendlyFireEnabled { get; set; } = true;
    public int FriendlyFireDamageThreshold { get; set; } = 40;

    public bool FailedClutchEnabled { get; set; } = true;
    public int FailedClutchEnemyHpThreshold { get; set; } = 50;

    public bool FirstBloodAgainstEnabled { get; set; } = true;
    public int FirstBloodAgainstTimeSeconds { get; set; } = 8;

    public bool BombDropDeathEnabled { get; set; } = true;

    public bool TeamFlashEnabled { get; set; } = true;
    public int TeamFlashMinTeammatesBlinded { get; set; } = 2;

    public bool TeamMolotovEnabled { get; set; } = true;

    public bool WastedGrenadeEnabled { get; set; } = true;

    public bool LowDamageGrenadeEnabled { get; set; } = false;
    public int LowDamageGrenadeThreshold { get; set; } = 20;
}
