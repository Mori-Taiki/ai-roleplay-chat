
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiRoleplayChat.Backend.Domain.Entities;

public class UserSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty; // e.g., "Gemini", "Replicate"

    [Required]
    [StringLength(100)]
    public string SettingKey { get; set; } = string.Empty; // e.g., "ChatModel", "ImagePromptGenerationModel"

    [Required]
    [StringLength(255)]
    public string SettingValue { get; set; } = string.Empty; // The actual model ID or version
}
