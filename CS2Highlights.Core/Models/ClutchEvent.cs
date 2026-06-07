using CS2Highlights.Core.Enums;

namespace CS2Highlights.Core.Models;

public class ClutchEvent : PlayerEvent
{
    public int EnemyCount { get; set; }
    public ClutchResult Result { get; set; }
    public int TickResolved { get; set; }
}
