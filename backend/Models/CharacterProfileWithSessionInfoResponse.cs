using System;

namespace AiRoleplayChat.Backend.Models
{
    public record CharacterProfileWithSessionInfoResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Personality { get; set; }
        public string? Tone { get; set; }
        public string? Backstory { get; set; }
        public string? SystemPrompt { get; set; }
        public string? ExampleDialogue { get; set; }
        public string? AvatarImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemPromptCustomized { get; set; }
        public string? SessionId { get; set; }
        public string? LastMessageSnippet { get; set; }
        public string? Appearance { get; set; }
        public string? UserAppellation { get; set; }
        public AiGenerationSettingsResponse? AiSettings { get; set; }
    }
}