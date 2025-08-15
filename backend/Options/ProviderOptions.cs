namespace AiRoleplayChat.Backend.Options;

/// <summary>
/// Configuration options for AI model providers
/// Defines default providers and models for text and image generation
/// </summary>
public class ProviderOptions
{
    public const string SectionName = "Providers";

    /// <summary>
    /// Default configuration for providers
    /// </summary>
    public DefaultOptions Default { get; set; } = new();

    /// <summary>
    /// Available text model providers
    /// </summary>
    public Dictionary<string, TextModelConfig> TextModels { get; set; } = new();

    /// <summary>
    /// Available image model providers
    /// </summary>
    public Dictionary<string, ImageModelConfig> ImageModels { get; set; } = new();
}

/// <summary>
/// Default provider settings
/// </summary>
public class DefaultOptions
{
    /// <summary>
    /// Default text model provider name
    /// </summary>
    public string TextProvider { get; set; } = "Gemini";

    /// <summary>
    /// Default text model ID
    /// </summary>
    public string TextModel { get; set; } = "gemini-1.5-flash-latest";

    /// <summary>
    /// Default image model provider name
    /// </summary>
    public string ImageProvider { get; set; } = "Replicate";

    /// <summary>
    /// Default image model ID
    /// </summary>
    public string ImageModel { get; set; } = "0fc0fa9885b284901a6f9c0b4d67701fd7647d157b88371427d63f8089ce140e";
}

/// <summary>
/// Configuration for a text model
/// </summary>
public class TextModelConfig
{
    /// <summary>
    /// Model identifier
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider service name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum tokens for this model
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Default temperature for this model
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Whether this model is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Configuration for an image model
/// </summary>
public class ImageModelConfig
{
    /// <summary>
    /// Model identifier
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider service name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Default image width
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Default image height
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Whether this model is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}