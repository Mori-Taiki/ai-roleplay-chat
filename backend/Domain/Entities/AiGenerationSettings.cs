using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiRoleplayChat.Backend.Domain.Entities
{
    /// <summary>
    /// AI generation settings that can be shared between users and character profiles
    /// </summary>
    public class AiGenerationSettings
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Indicates whether these settings belong to a user ("User") or a character ("Character")
        /// </summary>
        [Required]
        [StringLength(20)]
        public string SettingsType { get; set; } = string.Empty;

        /// <summary>
        /// Provider for chat text generation (e.g., "Gemini", "OpenAI")
        /// </summary>
        [StringLength(50)]
        public string? ChatGenerationProvider { get; set; }

        /// <summary>
        /// Model used for chat text generation (e.g., "gemini-1.5-flash-latest", "gpt-4")
        /// </summary>
        [StringLength(200)]
        public string? ChatGenerationModel { get; set; }

        /// <summary>
        /// Provider for generating image prompts from text (e.g., "Gemini", "OpenAI")
        /// </summary>
        [StringLength(50)]
        public string? ImagePromptGenerationProvider { get; set; }

        /// <summary>
        /// Model used for generating image prompts from text (e.g., "gemini-1.5-flash-latest")
        /// </summary>
        [StringLength(200)]
        public string? ImagePromptGenerationModel { get; set; }

        /// <summary>
        /// Provider for actual image generation (e.g., "Replicate", "Stability")
        /// </summary>
        [StringLength(50)]
        public string? ImageGenerationProvider { get; set; }

        /// <summary>
        /// Model used for actual image generation (e.g., "black-forest-labs/flux-1-dev")
        /// </summary>
        [StringLength(200)]
        public string? ImageGenerationModel { get; set; }

        /// <summary>
        /// Custom instruction text for generating image prompts
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? ImageGenerationPromptInstruction { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}