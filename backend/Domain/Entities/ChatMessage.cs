using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiRoleplayChat.Backend.Domain.Entities;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    // ★ SessionId (FK) を追加
    [Required]
    [StringLength(36)]
    public string SessionId { get; set; } = string.Empty;

    // ★ ChatSession へのナビゲーションプロパティを追加
    [ForeignKey(nameof(SessionId))]
    public virtual ChatSession Session { get; set; } = null!;

    [Required]
    public int CharacterProfileId { get; set; }

    [ForeignKey(nameof(CharacterProfileId))]
    public virtual CharacterProfile CharacterProfile { get; set; } = null!;

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Sender { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Text { get; set; } = string.Empty;

    [Column(TypeName = "MEDIUMTEXT")]
    public string? ImageUrl { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ImagePrompt { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ModelId { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ServiceName { get; set; }

    [Column(TypeName = "DATETIME(6)")]
    public DateTime? DeletedAt { get; set; }

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "DATETIME(6)")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}