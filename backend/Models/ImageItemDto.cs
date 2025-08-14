using System;

namespace AiRoleplayChat.Backend.Models
{
    public class ImageItemDto
    {
        public int MessageId { get; set; }
        public int CharacterId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string? SessionTitle { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? ImagePrompt { get; set; }
        public string? ModelId { get; set; }
        public string? ServiceName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}