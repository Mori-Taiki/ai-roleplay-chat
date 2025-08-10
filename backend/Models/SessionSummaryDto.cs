using System;

namespace AiRoleplayChat.Backend.Models
{
    public class SessionSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}