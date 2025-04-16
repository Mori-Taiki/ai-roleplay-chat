namespace AiRoleplayChat.Backend.Models;

public record ChatResponse(
    string Reply,
    string SessionId,
    string? ImageUrl
);