// using ディレクティブ (必要なものを確認)
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ILogger を使う場合

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IGeminiService geminiService, ILogger<ChatController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    // POST /api/chat
    [HttpPost]
    public async Task<IActionResult> PostChat([FromBody] ChatRequest request) 
    {
        // ログ出力 (ILogger を使用)
        _logger.LogInformation("POST /api/chat called with prompt: {Prompt}", request.Prompt);
        try
        {
            // 引数チェック (必要であれば)
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                _logger.LogWarning("Received empty prompt for /api/chat.");
                return BadRequest(new { Message = "Prompt cannot be empty." });
            }

            // 注入された _geminiService を使用 
            string replyText = await _geminiService.GenerateChatResponseAsync(request.Prompt);

            return Ok(new ChatResponse(replyText));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing /api/chat request for prompt: {Prompt}", request.Prompt);

            // ControllerBase.Problem() を使用して 500 エラー応答を返す
            // Problem() は標準的なエラー応答形式 (ProblemDetails) を生成します
            return Problem(
                title: "An error occurred while generating chat response.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError 
            );
        }
    }
}