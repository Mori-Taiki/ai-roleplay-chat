using AiRoleplayChat.Backend.Data;     // AppDbContext の名前空間
using AiRoleplayChat.Backend.Domain.Entities; // CharacterProfile の名前空間
using AiRoleplayChat.Backend.Models; // CreateCharacterProfileRequest の名前空間
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Include this for ToListAsync, FindAsync etc.

namespace AiRoleplayChat.Backend.Controllers; // プロジェクトの実際の名前空間に合わせてください

[ApiController]
[Route("api/[controller]")]
public class CharacterProfilesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatController> _logger;

    public CharacterProfilesController(AppDbContext context, ILogger<ChatController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // POST: api/characterprofiles
    [HttpPost(Name = "CreateCharacterProfile")]
    [ProducesResponseType(typeof(CharacterProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CharacterProfileResponse>> CreateCharacterProfile(
        [FromBody] CreateCharacterProfileRequest request)
    {
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
            UserId = 1
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CharacterProfileResponse>>> GetAllCharacterProfiles()
    {
        var profiles = await _context.CharacterProfiles
            // .Where(p => p.UserId == 1) // TODO: 将来的に認証と連携し、ログインユーザーのプロファイルのみ取得する場合は Where句 を追加
            .OrderBy(p => p.Name) // 例: 名前順でソート
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CharacterProfileResponse>> GetCharacterProfile(int id)
    {
        var profile = await _context.CharacterProfiles.FindAsync(id);

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
    public async Task<IActionResult> UpdateCharacterProfile(int id, [FromBody] UpdateCharacterProfileRequest request)
    {
        // まず、指定された ID のエンティティが存在するか確認
        var existingProfile = await _context.CharacterProfiles.FindAsync(id);

        if (existingProfile == null)
        {
            return NotFound();
        }

        // --- オプション: より堅牢にするなら UserId のチェックもここで行う ---
        // if (existingProfile.UserId != 1) // 仮のユーザーIDと比較
        // {
        //     // 権限がない、または操作対象が違う場合は 403 Forbidden や 404 NotFound を返す
        //     return Forbid(); // または NotFound();
        // }
        // ------

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
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)] // 成功 (削除完了、ボディなし)
    [ProducesResponseType(StatusCodes.Status404NotFound)] // 削除対象が存在しない
    public async Task<IActionResult> DeleteCharacterProfile(int id)
    {
        // 削除対象のエンティティをDBから取得
        var profileToDelete = await _context.CharacterProfiles.FindAsync(id);

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