using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiRoleplayChat.Backend.Domain.Entities;

public class ChatSession
{
    [Key]
    [StringLength(36)] // UUID の長さ
    public string Id { get; set; } = Guid.NewGuid().ToString(); // デフォルトで UUID を生成

    [Required]
    public int CharacterProfileId { get; set; }

    [ForeignKey(nameof(CharacterProfileId))]
    public virtual CharacterProfile CharacterProfile { get; set; } = null!;

    [Required]
    public int UserId { get; set; }

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "DATETIME(6)")]
    public DateTime? EndTime { get; set; } // NULL 許容

    [Column(TypeName = "JSON")] // MySQL の JSON 型を使用 (TEXT にする場合は変更)
    public string? Metadata { get; set; }

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ChatMessages へのナビゲーションプロパティ (1対多)
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}