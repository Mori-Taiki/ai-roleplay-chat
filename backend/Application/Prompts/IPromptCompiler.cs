using AiRoleplayChat.Backend.Domain.Entities;

namespace AiRoleplayChat.Backend.Application.Prompts;

/// <summary>
/// Service interface for compiling and managing prompts for AI interactions
/// </summary>
public interface IPromptCompiler
{
    /// <summary>
    /// Generate default system prompt for a character
    /// </summary>
    /// <param name="name">Character name</param>
    /// <param name="personality">Character personality</param>
    /// <param name="tone">Character tone</param>
    /// <param name="backstory">Character backstory</param>
    /// <param name="appearance">Character appearance</param>
    /// <param name="userAppellation">How character addresses the user</param>
    /// <returns>Generated system prompt</returns>
    string GenerateDefaultPrompt(string name, string? personality, string? tone, string? backstory, string? appearance = null, string? userAppellation = null);

    /// <summary>
    /// Append image generation instruction to a base prompt
    /// </summary>
    /// <param name="basePrompt">Base system prompt</param>
    /// <returns>Prompt with image generation instructions appended</returns>
    string AppendImageInstruction(string basePrompt);

    /// <summary>
    /// Get the image generation instruction text
    /// </summary>
    /// <returns>Image generation instruction</returns>
    string GetImageGenerationInstruction();
}