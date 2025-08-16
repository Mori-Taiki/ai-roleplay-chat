using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Application.Prompts;

namespace AiRoleplayChat.Backend.Controllers; // プロジェクトの実際の名前空間に合わせてください

[ApiController]
public class CharacterProfilesController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IPromptCompiler _promptCompiler;
    private readonly IAiGenerationSettingsService _aiSettingsService;

    public CharacterProfilesController(
        AppDbContext context, 
        IUserService userService, 
        IPromptCompiler promptCompiler, 
        IAiGenerationSettingsService aiSettingsService,
        ILogger<CharacterProfilesController> logger)
        : base(userService, logger)
    {
        _context = context;
        _promptCompiler = promptCompiler;
        _aiSettingsService = aiSettingsService;
    }

    // POST: api/characterprofiles
    [HttpPost(Name = "CreateCharacterProfile")]
    [ProducesResponseType(typeof(CharacterProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CharacterProfileResponse>> CreateCharacterProfile(
        [FromBody] CreateCharacterProfileRequest request, CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        string baseSystemPrompt;
        bool isCustomized;

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            baseSystemPrompt = request.SystemPrompt;
            isCustomized = true;
            _logger.LogInformation("Using user-provided SystemPrompt for character: {CharacterName}", request.Name);
        }
        else
        {
            // ★ 共通メソッドでデフォルトプロンプトを生成
            baseSystemPrompt = _promptCompiler.GenerateDefaultPrompt(
                request.Name, request.Personality, request.Tone, request.Backstory, request.Appearance, request.UserAppellation);
            isCustomized = false;
            _logger.LogInformation("Generating SystemPrompt based on other fields for character: {CharacterName}", request.Name);
        }

        // ★ ベースプロンプトに画像生成指示を追加
        // string finalSystemPrompt = SystemPromptHelper.AppendImageInstruction(baseSystemPrompt);

        // Create AI settings if provided
        int? aiSettingsId = null;
        if (request.AiSettings != null && 
            (!string.IsNullOrEmpty(request.AiSettings.ChatGenerationProvider) ||
             !string.IsNullOrEmpty(request.AiSettings.ChatGenerationModel) ||
             !string.IsNullOrEmpty(request.AiSettings.ImagePromptGenerationProvider) ||
             !string.IsNullOrEmpty(request.AiSettings.ImagePromptGenerationModel) ||
             !string.IsNullOrEmpty(request.AiSettings.ImageGenerationProvider) ||
             !string.IsNullOrEmpty(request.AiSettings.ImageGenerationModel) ||
             !string.IsNullOrEmpty(request.AiSettings.ImageGenerationPromptInstruction)))
        {
            var aiSettings = new AiGenerationSettings
            {
                SettingsType = "Character",
                ChatGenerationProvider = request.AiSettings.ChatGenerationProvider,
                ChatGenerationModel = request.AiSettings.ChatGenerationModel,
                ImagePromptGenerationProvider = request.AiSettings.ImagePromptGenerationProvider,
                ImagePromptGenerationModel = request.AiSettings.ImagePromptGenerationModel,
                ImageGenerationProvider = request.AiSettings.ImageGenerationProvider,
                ImageGenerationModel = request.AiSettings.ImageGenerationModel,
                ImageGenerationPromptInstruction = request.AiSettings.ImageGenerationPromptInstruction
            };

            var createdAiSettings = await _aiSettingsService.CreateOrUpdateSettingsAsync(aiSettings);
            aiSettingsId = createdAiSettings.Id;
        }

        var newProfile = new CharacterProfile
        {
            Name = request.Name,
            Personality = request.Personality,
            Tone = request.Tone,
            Backstory = request.Backstory,
            SystemPrompt = baseSystemPrompt,
            ExampleDialogue = request.ExampleDialogue,
            AvatarImageUrl = request.AvatarImageUrl,
            Appearance = request.Appearance,
            UserAppellation = request.UserAppellation,
            AiSettingsId = aiSettingsId,
            IsActive = true,
            IsSystemPromptCustomized = isCustomized,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = appUserId
        };

        _context.CharacterProfiles.Add(newProfile);
        await _context.SaveChangesAsync(cancellationToken);

        // Load the AI settings for the response
        AiGenerationSettingsResponse? aiSettingsResponse = null;
        if (newProfile.AiSettingsId.HasValue)
        {
            var aiSettings = await _aiSettingsService.GetSettingsAsync(newProfile.AiSettingsId.Value);
            if (aiSettings != null)
            {
                aiSettingsResponse = new AiGenerationSettingsResponse(
                    aiSettings.Id,
                    aiSettings.SettingsType,
                    aiSettings.ChatGenerationProvider,
                    aiSettings.ChatGenerationModel,
                    aiSettings.ImagePromptGenerationProvider,
                    aiSettings.ImagePromptGenerationModel,
                    aiSettings.ImageGenerationProvider,
                    aiSettings.ImageGenerationModel,
                    aiSettings.ImageGenerationPromptInstruction
                );
            }
        }

        var responseDto = new CharacterProfileResponse(
            newProfile.Id,
            newProfile.Name,
            newProfile.Personality,
            newProfile.Tone,
            newProfile.Backstory,
            newProfile.SystemPrompt,
            newProfile.ExampleDialogue,
            newProfile.AvatarImageUrl,
            newProfile.IsActive,
            newProfile.IsSystemPromptCustomized,
            newProfile.Appearance,
            newProfile.UserAppellation,
            aiSettingsResponse
        );

        return CreatedAtAction(nameof(GetCharacterProfile), new { id = newProfile.Id }, responseDto);
    }

    // GET: api/characterprofiles
    [HttpGet(Name = "GetAllCharacterProfiles")]
    [ProducesResponseType(typeof(IEnumerable<CharacterProfileWithSessionInfoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CharacterProfileWithSessionInfoResponse>>> GetAllCharacterProfiles(CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        var characters = await _context.CharacterProfiles
            .Include(p => p.AiSettings)
            .Where(p => p.UserId == appUserId)
            .OrderBy(p => p.Id)
            .ToListAsync();
        
            var responseList = new List<CharacterProfileWithSessionInfoResponse>();

            foreach (var character in characters)
            {
                // 各キャラクターについて、最新のセッションとメッセージを取得
                var latestSessionInfo = await _context.ChatSessions
                    .Where(s => s.CharacterProfileId == character.Id && s.UserId == appUserId) // ★ UserId プロパティ名要確認
                    .OrderByDescending(s => s.StartTime) // ★ StartTime プロパティ名要確認
                    .Select(s => new { // 必要な情報だけ取得
                        SessionId = s.Id,
                        LastMessage = s.Messages // セッション内のメッセージ
                                         .OrderByDescending(m => m.Timestamp) // 最新のメッセージ
                                         .Select(m => new { m.Text, m.Timestamp }) // メッセージ内容とタイムスタンプ
                                         .FirstOrDefault() // 最新の1件を取得
                    })
                    .FirstOrDefaultAsync(); // 最新のセッションを1件取得

                AiGenerationSettingsResponse? aiSettingsResponse = null;
                if (character.AiSettings != null)
                {
                    aiSettingsResponse = new AiGenerationSettingsResponse(
                        character.AiSettings.Id,
                        character.AiSettings.SettingsType,
                        character.AiSettings.ChatGenerationProvider,
                        character.AiSettings.ChatGenerationModel,
                        character.AiSettings.ImagePromptGenerationProvider,
                        character.AiSettings.ImagePromptGenerationModel,
                        character.AiSettings.ImageGenerationProvider,
                        character.AiSettings.ImageGenerationModel,
                        character.AiSettings.ImageGenerationPromptInstruction
                    );
                }

                var response = new CharacterProfileWithSessionInfoResponse
                {
                    Id = character.Id,
                    Name = character.Name,
                    Personality = character.Personality,
                    Tone = character.Tone,
                    Backstory = character.Backstory,
                    SystemPrompt = character.SystemPrompt,
                    ExampleDialogue = character.ExampleDialogue,
                    AvatarImageUrl = character.AvatarImageUrl,
                    IsActive = character.IsActive,
                    IsSystemPromptCustomized = character.IsSystemPromptCustomized,
                    Appearance = character.Appearance,
                    UserAppellation = character.UserAppellation,
                    AiSettings = aiSettingsResponse,
                    SessionId = latestSessionInfo?.SessionId,
                    // 最新メッセージの一部を取得 (例: 50文字に制限)
                    LastMessageSnippet = latestSessionInfo?.LastMessage?.Text?.Length > 1000
                        ? latestSessionInfo.LastMessage.Text.Substring(0, 1000) + "..."
                        : latestSessionInfo?.LastMessage?.Text
                };
                responseList.Add(response);
            }

        return Ok(responseList);
    }

    // GET: api/characterprofiles/{id}
    [HttpGet("{id}", Name = "GetCharacterProfile")]
    [ProducesResponseType(typeof(CharacterProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CharacterProfileResponse>> GetCharacterProfile(int id, CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        var profile = await _context.CharacterProfiles
            .Include(p => p.AiSettings)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUserId, cancellationToken);

        if (profile == null)
        {
            return NotFound();
        }

        AiGenerationSettingsResponse? aiSettingsResponse = null;
        if (profile.AiSettings != null)
        {
            aiSettingsResponse = new AiGenerationSettingsResponse(
                profile.AiSettings.Id,
                profile.AiSettings.SettingsType,
                profile.AiSettings.ChatGenerationProvider,
                profile.AiSettings.ChatGenerationModel,
                profile.AiSettings.ImagePromptGenerationProvider,
                profile.AiSettings.ImagePromptGenerationModel,
                profile.AiSettings.ImageGenerationProvider,
                profile.AiSettings.ImageGenerationModel,
                profile.AiSettings.ImageGenerationPromptInstruction
            );
        }

        var response = new CharacterProfileResponse(
            profile.Id,
            profile.Name,
            profile.Personality,
            profile.Tone,
            profile.Backstory,
            profile.SystemPrompt,
            profile.ExampleDialogue,
            profile.AvatarImageUrl,
            profile.IsActive,
            profile.IsSystemPromptCustomized,
            profile.Appearance,
            profile.UserAppellation,
            aiSettingsResponse
        );

        return Ok(response);
    }

    // PUT: api/characterprofiles/{id}
    [HttpPut("{id}", Name = "UpdateCharacterProfile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacterProfile(int id, [FromBody] UpdateCharacterProfileRequest request, CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        var existingProfile = await _context.CharacterProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUserId.Value, cancellationToken); // .Value

        if (existingProfile == null) return NotFound();

        string baseSystemPrompt; // 画像指示追加前のベースプロンプト

        if (request.IsSystemPromptCustomized && !string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            baseSystemPrompt = request.SystemPrompt ?? ""; // null チェック
            _logger.LogInformation("Updating Character {Id} with user-customized SystemPrompt.", id);
        }
        else
        {
            // ★ 共通メソッドでデフォルトプロンプトを生成
            baseSystemPrompt = _promptCompiler.GenerateDefaultPrompt(
                request.Name, request.Personality, request.Tone, request.Backstory, request.Appearance, request.UserAppellation); // 更新リクエストの値を使う
            _logger.LogInformation("Auto-generating SystemPrompt for Character {Id} based on other fields.", id);
        }

        // ★ ベースプロンプトに画像生成指示を追加
        // string finalSystemPrompt = SystemPromptHelper.AppendImageInstruction(baseSystemPrompt);

        // Handle AI settings update
        if (request.AiSettings != null)
        {
            if (existingProfile.AiSettingsId.HasValue)
            {
                // Update existing AI settings
                var existingAiSettings = await _aiSettingsService.GetSettingsAsync(existingProfile.AiSettingsId.Value);
                if (existingAiSettings != null)
                {
                    existingAiSettings.ChatGenerationProvider = request.AiSettings.ChatGenerationProvider;
                    existingAiSettings.ChatGenerationModel = request.AiSettings.ChatGenerationModel;
                    existingAiSettings.ImagePromptGenerationProvider = request.AiSettings.ImagePromptGenerationProvider;
                    existingAiSettings.ImagePromptGenerationModel = request.AiSettings.ImagePromptGenerationModel;
                    existingAiSettings.ImageGenerationProvider = request.AiSettings.ImageGenerationProvider;
                    existingAiSettings.ImageGenerationModel = request.AiSettings.ImageGenerationModel;
                    existingAiSettings.ImageGenerationPromptInstruction = request.AiSettings.ImageGenerationPromptInstruction;
                    await _aiSettingsService.CreateOrUpdateSettingsAsync(existingAiSettings);
                }
            }
            else
            {
                // Create new AI settings
                var newAiSettings = new AiGenerationSettings
                {
                    SettingsType = "Character",
                    ChatGenerationProvider = request.AiSettings.ChatGenerationProvider,
                    ChatGenerationModel = request.AiSettings.ChatGenerationModel,
                    ImagePromptGenerationProvider = request.AiSettings.ImagePromptGenerationProvider,
                    ImagePromptGenerationModel = request.AiSettings.ImagePromptGenerationModel,
                    ImageGenerationProvider = request.AiSettings.ImageGenerationProvider,
                    ImageGenerationModel = request.AiSettings.ImageGenerationModel,
                    ImageGenerationPromptInstruction = request.AiSettings.ImageGenerationPromptInstruction
                };
                var createdAiSettings = await _aiSettingsService.CreateOrUpdateSettingsAsync(newAiSettings);
                existingProfile.AiSettingsId = createdAiSettings.Id;
            }
        }
        else if (existingProfile.AiSettingsId.HasValue)
        {
            // Remove AI settings if request doesn't include them
            await _aiSettingsService.DeleteSettingsAsync(existingProfile.AiSettingsId.Value);
            existingProfile.AiSettingsId = null;
        }

        // 既存エンティティのプロパティをリクエスト DTO の値で更新
        existingProfile.Name = request.Name;
        existingProfile.Personality = request.Personality;
        existingProfile.Tone = request.Tone;
        existingProfile.Backstory = request.Backstory;
        existingProfile.SystemPrompt = baseSystemPrompt;
        existingProfile.ExampleDialogue = request.ExampleDialogue;
        existingProfile.AvatarImageUrl = request.AvatarImageUrl;
        existingProfile.Appearance = request.Appearance;
        existingProfile.UserAppellation = request.UserAppellation;
        existingProfile.IsActive = request.IsActive;
        existingProfile.IsSystemPromptCustomized = request.IsSystemPromptCustomized;
        existingProfile.UpdatedAt = DateTime.UtcNow; // 更新日時を更新

        try
        {
            // 変更をデータベースに保存
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // 同時実行制御の例外処理 (必要であれば)
            // 例えば、更新しようとしたレコードが既に他のトランザクションで削除されていた場合など
            if (!await CharacterProfileExists(id)) // 存在確認ヘルパーメソッド (後述)
            {
                return NotFound();
            }
            else
            {
                throw; // その他の同時実行例外は再スロー
            }
        }

        // 成功したら 204 No Content を返す (レスポンスボディはなし)
        return NoContent();
    }

    // Update メソッド内で使うヘルパーメソッド (存在確認用)
    private async Task<bool> CharacterProfileExists(int id)
    {
        var (appUserId, _) = await GetCurrentAppUserIdAsync();
        if (appUserId == null) return false;
        return await _context.CharacterProfiles.AnyAsync(e => e.Id == id && e.UserId == appUserId.Value);
    }


    // DELETE: api/characterprofiles/{id}
    [HttpDelete("{id}", Name = "DeleteCharacterProfile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCharacterProfile(int id, CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        // 削除対象のエンティティをDBから取得
        var profileToDelete = await _context.CharacterProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUserId, cancellationToken);

        if (profileToDelete == null)
        {
            // 対象が見つからなければ 404 Not Found
            return NotFound();
        }

        // DbContext からエンティティを削除対象としてマーク
        _context.CharacterProfiles.Remove(profileToDelete);

        // 変更をデータベースに保存 (DELETE文が実行される)
        await _context.SaveChangesAsync();

        // 成功したら 204 No Content を返すのが一般的
        return NoContent();
    }

    
}