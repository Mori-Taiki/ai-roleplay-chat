using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Domain.Entities;

[Index(nameof(B2cObjectId), IsUnique = true)] // B2cObjectId にユニークインデックスを作成
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(36)]
    public string B2cObjectId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(255)]
    // Email は必須ではないかもしれない & B2C側で一意性は保証されるはずなので Unique 制約は任意
    public string? Email { get; set; }

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 将来的に他のエンティティとのリレーションを追加する可能性
    // public virtual ICollection<CharacterProfile> CharacterProfiles { get; set; } = new List<CharacterProfile>();
    // public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}