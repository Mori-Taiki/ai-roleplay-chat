using AiRoleplayChat.Backend.Models; // ImageGenRequest, ImageGenerationResponse
using AiRoleplayChat.Backend.Services; // IGeminiService, IImagenService
using Microsoft.AspNetCore.Mvc; // Controller関連
using Microsoft.Extensions.Logging; // ILogger

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly IImagenService _imagenService;
    private readonly ILogger<ImageController> _logger;

    // コンストラクタで必要なサービスとロガーを受け取る
    public ImageController(
        IGeminiService geminiService,
        IImagenService imagenService,
        ILogger<ImageController> logger)
    {
        _geminiService = geminiService;
        _imagenService = imagenService;
        _logger = logger;
    }

    // POST /api/image
    [HttpPost("generate")] // アクション名を付ける場合: POST /api/image/generate
    // または [HttpPost] だけなら POST /api/image
    // [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ImageGenerationResponse))]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateImage([FromBody] ImageGenRequest request)
    {
        _logger.LogInformation("POST /api/image/generate called with prompt: {JapanesePrompt}", request.Prompt);

        try
        {
            string japanesePrompt = request.Prompt;
            if (string.IsNullOrWhiteSpace(japanesePrompt))
            {
                _logger.LogWarning("Received empty prompt for image generation.");
                return BadRequest(new { Message = "プロンプトが空です。" });
            }

            // --- 1. 翻訳 (GeminiService を使う) ---
            string englishPrompt;
            try
            {
                _logger.LogInformation("Translating Japanese prompt: \"{JapanesePrompt}\"...", japanesePrompt);
                // 注入された _geminiService を使用
                englishPrompt = await _geminiService.TranslateToEnglishAsync(japanesePrompt);
                _logger.LogInformation("Translation successful: \"{EnglishPrompt}\"", englishPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during translation for image generation. Japanese prompt: {JapanesePrompt}", japanesePrompt);
                return Problem("プロンプトの翻訳中にエラーが発生しました。", statusCode: StatusCodes.Status500InternalServerError);
            }


            // --- 2. 画像生成 (ImagenService を使う) ---
            ImageGenerationResponse imageResponse;
            try
            {
                _logger.LogInformation("Requesting image generation with prompt: \"{EnglishPrompt}\"...", englishPrompt);
                // 注入された _imagenService を使用
                imageResponse = await _imagenService.GenerateImageAsync(englishPrompt);
                _logger.LogInformation("Image generation successful!");
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error occurred during image generation with prompt: {EnglishPrompt}", englishPrompt);
                 // ControllerBase.Problem() を使用
                return Problem("画像生成中にエラーが発生しました。", statusCode: StatusCodes.Status500InternalServerError);
            }

            // --- 3. レスポンスを返す ---
            return Ok(imageResponse);
        }
        catch (Exception ex) // その他の予期せぬエラー
        {
            // このcatchは通常、上のtry-catchで捕捉されるはずだが、念のため
            _logger.LogError(ex, "Unexpected error in GenerateImage action for prompt: {JapanesePrompt}", request.Prompt);
            return Problem("画像生成リクエストの処理中に予期せぬエラーが発生しました。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}