namespace AiRoleplayChat.Backend.Application.Contracts;

/// <summary>
/// Represents a single conversation turn (user input + AI response)
/// </summary>
public record ChatTurn(
    string UserMessage,
    string? AssistantMessage = null,
    DateTime? Timestamp = null
);

/// <summary>
/// Request for text generation
/// </summary>
public record TextRequest(
    string Prompt,
    string? SystemPrompt = null,
    List<ChatTurn>? History = null,
    int? MaxTokens = null,
    double? Temperature = null,
    int? UserId = null
);

/// <summary>
/// Result of text generation
/// </summary>
public record TextCompletion(
    string Text,
    string ModelId,
    string ServiceName,
    int? TokensUsed = null,
    double? ConfidenceScore = null
);

/// <summary>
/// Represents a delta/chunk in streaming text generation
/// </summary>
public record TextDelta(
    string Content,
    bool IsComplete = false,
    string? FinishReason = null
);

/// <summary>
/// Request for image generation
/// </summary>
public record ImageRequest(
    string Prompt,
    string? NegativePrompt = null,
    int? Width = null,
    int? Height = null,
    int? UserId = null
);

/// <summary>
/// Result of image generation
/// </summary>
public record ImageResult(
    byte[]? ImageBytes,
    string? MimeType,
    string ModelId,
    string ServiceName,
    string ActualPrompt,
    int? Width = null,
    int? Height = null
);

/// <summary>
/// Capabilities and metadata of a model
/// </summary>
public record ModelCapabilities(
    string ModelId,
    string ServiceName,
    bool SupportsStreaming = false,
    bool SupportsImages = false,
    int? MaxTokens = null,
    List<string>? SupportedFormats = null
);