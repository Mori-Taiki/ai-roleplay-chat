namespace AiRoleplayChat.Backend.Models;

public record ChatResponse(
    string Reply,
    string SessionId,
    int AiMessageId,
    bool RequiresImageGeneration
);