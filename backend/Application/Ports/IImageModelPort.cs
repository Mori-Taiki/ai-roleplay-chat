using AiRoleplayChat.Backend.Application.Contracts;

namespace AiRoleplayChat.Backend.Application.Ports;

/// <summary>
/// Port interface for image generation models
/// Abstracts away specific AI provider implementations
/// </summary>
public interface IImageModelPort
{
    /// <summary>
    /// Generate image based on the given request
    /// </summary>
    /// <param name="request">Image generation request containing prompt and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image generation result</returns>
    Task<ImageResult?> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the capabilities of this image model
    /// </summary>
    /// <returns>Model capabilities information</returns>
    ModelCapabilities GetCapabilities();
}