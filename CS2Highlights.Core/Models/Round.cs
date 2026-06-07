using CS2Highlights.Core.Enums;

namespace CS2Highlights.Core.Models;

public class Round
{
    public int RoundNumber { get; set; }
    public int TickStart { get; set; }
    public int TickEnd { get; set; }
    public TeamSide WinnerSide { get; set; }
}
