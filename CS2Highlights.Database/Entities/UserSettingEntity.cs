using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS2Highlights.Database.Entities;

[Table("UserSettings")]
public class UserSettingEntity
{
    [Key]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}
