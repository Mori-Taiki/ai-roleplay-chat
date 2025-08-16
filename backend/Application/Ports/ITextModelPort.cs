using AiRoleplayChat.Backend.Application.Contracts;

namespace AiRoleplayChat.Backend.Application.Ports;

/// <summary>
/// Port interface for text generation models (LLM)
/// Abstracts away specific AI provider implementations
/// </summary>
public interface ITextModelPort
{
    /// <summary>
    /// Generate text completion based on the given request
    /// </summary>
    /// <param name="request">Text generation request containing prompt and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Text completion result</returns>
    Task<TextCompletion> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the capabilities of this text model
    /// </summary>
    /// <returns>Model capabilities information</returns>
    ModelCapabilities GetCapabilities();
}