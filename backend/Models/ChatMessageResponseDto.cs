public class ChatMessageResponseDto
{
    public int Id { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime Timestamp { get; set; }
}