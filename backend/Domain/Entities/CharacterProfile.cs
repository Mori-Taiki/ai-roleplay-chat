using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiRoleplayChat.Backend.Domain.Entities
{
    public class CharacterProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "TEXT")]
        public string? Personality { get; set; } 

        [Column(TypeName = "TEXT")]
        public string? Tone { get; set; } 

        [Column(TypeName = "TEXT")]
        public string? Backstory { get; set; } 

        [Column(TypeName = "TEXT")]
        public string? SystemPrompt { get; set; } 

        [Column(TypeName = "JSON")] // MySQLのJSON型に対応 (なければTEXTでも可)
        public string? ExampleDialogue { get; set; } 

        [StringLength(2083)] // URLの最大長を考慮
        public string? AvatarImageUrl { get; set; } 

        [Required]
        public bool IsActive { get; set; } = true; // デフォルト値を設定

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // デフォルト値を設定 (UTC推奨)

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // デフォルト値を設定 (DB側で自動更新されるが、初期値として設定)

        public int? UserId { get; set; }
        // public virtual User? User { get; set; } // ナビゲーションプロパティ (Userエンティティが存在する場合)
    }
}