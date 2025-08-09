using System;

namespace AiRoleplayChat.Backend.Models
{
    public class ChatSessionResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public int CharacterProfileId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LastMessageSnippet { get; set; }
        public int MessageCount { get; set; }
    }
}