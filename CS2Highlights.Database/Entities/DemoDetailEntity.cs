using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS2Highlights.Database.Entities;

[Table("DemoDetails")]
public class DemoDetailEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    public string MapName { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }

    // JSON array of {SteamId, PlayerName, Team}
    public string PlayersJson { get; set; } = "[]";
}
