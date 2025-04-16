using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AiRoleplayChat.Backend.Controllers; // プロジェクトの実際の名前空間に合わせてください

[ApiController]
public class CharacterProfilesController : BaseApiController
{
    private readonly AppDbContext _context;

    public CharacterProfilesController(AppDbContext context, IUserService userService, ILogger<CharacterProfilesController> logger)
        : base(userService, logger)
    {
        _context = context;
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

        string systemPrompt;
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            // リクエストに SystemPrompt が含まれていれば、それを使用する
            systemPrompt = request.SystemPrompt;
            _logger.LogInformation("Using user-provided SystemPrompt for character: {CharacterName}", request.Name);
        }
        else
        {
            _logger.LogInformation("Generating SystemPrompt based on other fields for character: {CharacterName}", request.Name);
            systemPrompt = $"あなたはキャラクター「{request.Name}」です。\n" +
                               $"性格: {request.Personality ?? "未設定"}\n" +
                               $"口調: {request.Tone ?? "未設定"}\n" +
                               $"背景: {request.Backstory ?? "未設定"}\n" +
                               "ユーザーと自然で魅力的な対話を行ってください。";
        }

        // 受け取った DTO と生成した SystemPrompt から CharacterProfile エンティティを作成
        var newProfile = new CharacterProfile
        {
            Name = request.Name,
            Personality = request.Personality,
            Tone = request.Tone,
            Backstory = request.Backstory,
            SystemPrompt = systemPrompt,
            ExampleDialogue = request.ExampleDialogue,
            AvatarImageUrl = request.AvatarImageUrl,
            IsActive = true,
            IsSystemPromptCustomized = !string.IsNullOrWhiteSpace(request.SystemPrompt),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = appUserId
        };

        // DbContext を通じてデータベースに追加
        _context.CharacterProfiles.Add(newProfile);
        await _context.SaveChangesAsync();

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
            newProfile.IsSystemPromptCustomized
        );

        return CreatedAtAction(nameof(GetCharacterProfile), new { id = newProfile.Id }, responseDto);
    }

    // GET: api/characterprofiles
    [HttpGet(Name = "GetAllCharacterProfiles")]
    [ProducesResponseType(typeof(IEnumerable<CharacterProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CharacterProfileResponse>>> GetAllCharacterProfiles(CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        var profiles = await _context.CharacterProfiles
            .Where(p => p.UserId == appUserId)
            .OrderBy(p => p.Id)
            .Select(p => new CharacterProfileResponse(
                p.Id,
                p.Name,
                p.Personality,
                p.Tone,
                p.Backstory,
                p.SystemPrompt,
                p.ExampleDialogue,
                p.AvatarImageUrl,
                p.IsActive,
                p.IsSystemPromptCustomized
            ))
            .ToListAsync();

        return Ok(profiles);
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
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUserId, cancellationToken);

        if (profile == null)
        {
            return NotFound();
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
            profile.IsSystemPromptCustomized
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

        // まず、指定された ID のエンティティが存在するか確認
        var existingProfile = await _context.CharacterProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUserId, cancellationToken);

        if (existingProfile == null)
        {
            return NotFound();
        }

        string finalSystemPrompt; // 最終的に保存する SystemPrompt
        if (request.IsSystemPromptCustomized && !string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            finalSystemPrompt = request.SystemPrompt ?? "";
            _logger.LogInformation("Updating Character {Id} with user-customized SystemPrompt.", id);
        }
        else
        {
            // --- 自動生成 ---
            _logger.LogInformation("Auto-generating SystemPrompt for Character {Id} based on other fields.", id);
            finalSystemPrompt = $"あなたはキャラクター「{request.Name}」です。\n" +
                                $"性格: {request.Personality ?? "未設定"}\n" +
                                $"口調: {request.Tone ?? "未設定"}\n" +
                                $"背景: {request.Backstory ?? "未設定"}\n" +
                                "ユーザーと自然で魅力的な対話を行ってください。";
        }

        // 既存エンティティのプロパティをリクエスト DTO の値で更新
        existingProfile.Name = request.Name;
        existingProfile.Personality = request.Personality;
        existingProfile.Tone = request.Tone;
        existingProfile.Backstory = request.Backstory;
        existingProfile.SystemPrompt = finalSystemPrompt;
        existingProfile.ExampleDialogue = request.ExampleDialogue;
        existingProfile.AvatarImageUrl = request.AvatarImageUrl;
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
        return await _context.CharacterProfiles.AnyAsync(e => e.Id == id);
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