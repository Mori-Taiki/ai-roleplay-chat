using System.ComponentModel.DataAnnotations;

namespace AiRoleplayChat.Backend.Models;

public class EditMessageRequest
{
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string NewText { get; set; } = string.Empty;
}