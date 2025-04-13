using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Data;

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ChatController> _logger;
    private readonly AppDbContext _context;

    public ChatController(IGeminiService geminiService, ILogger<ChatController> logger, AppDbContext context)
    {
        _geminiService = geminiService;
        _logger = logger;
        _context = context;
    }

    // POST /api/chat
    [HttpPost]
    public async Task<IActionResult> PostChat([FromBody] ChatRequest request)
    {
        // ログ出力 (ILogger を使用)
        _logger.LogInformation("POST /api/chat called with prompt: {Prompt}", request.Prompt);

        // --- 1. CharacterProfile を DB から取得 ---
        var characterProfile = await _context.CharacterProfiles.FindAsync(request.CharacterProfileId);

        // --- 2. 存在チェック ---
        if (characterProfile == null)
        {
            // 有効な CharacterProfileId でない場合は 404 エラーを返す
            return NotFound(new { message = $"指定されたキャラクターID ({request.CharacterProfileId}) は見つかりません。" });
        }
        // 必要であれば IsActive フラグなどもチェックする
        // if (!characterProfile.IsActive) { ... }

        // --- 3. SystemPrompt を取得 ---
        var systemPrompt = characterProfile.SystemPrompt;

        // SystemPrompt が空の場合のフォールバック処理（任意）
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            // ログを出力したり、デフォルトのプロンプトを設定したりする
            Console.WriteLine($"Warning: CharacterProfile Id={characterProfile.Id} の SystemPrompt が空です。デフォルトを使用します。");
            systemPrompt = "あなたはユーザーと会話する、親切なAIアシスタントです。"; // 仮のデフォルト値
        }

        // --- 4. GeminiService 呼び出し  ---
        try
        {
            // 引数チェック (必要であれば)
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                _logger.LogWarning("Received empty prompt for /api/chat.");
                return BadRequest(new { Message = "Prompt cannot be empty." });
            }

            // 注入された _geminiService を使用 
            string replyText = await _geminiService.GenerateChatResponseAsync(request.Prompt, systemPrompt);

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