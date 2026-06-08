using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS2Highlights.Database.Entities;

[Table("Matches")]
public class MatchEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string MatchId { get; set; } = string.Empty;

    [Required]
    public string DemoPath { get; set; } = string.Empty;

    [Required]
    public string DemoFileName { get; set; } = string.Empty;

    [Required]
    public string Map { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [Required]
    public string SelectedPlayerSteamId { get; set; } = string.Empty;

    [Required]
    public string SelectedPlayerName { get; set; } = string.Empty;

    public DateTime? ParsedAt { get; set; }

    public ICollection<RoundEntity> Rounds { get; set; } = [];
    public ICollection<KillEventEntity> KillEvents { get; set; } = [];
    public ICollection<GrenadeEventEntity> GrenadeEvents { get; set; } = [];
    public ICollection<HighlightEntity> Highlights { get; set; } = [];
}
