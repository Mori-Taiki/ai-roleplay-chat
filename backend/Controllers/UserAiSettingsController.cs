using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserAiSettingsController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IAiGenerationSettingsService _aiSettingsService;

    public UserAiSettingsController(
        AppDbContext context, 
        IUserService userService, 
        IAiGenerationSettingsService aiSettingsService,
        ILogger<UserAiSettingsController> logger)
        : base(userService, logger)
    {
        _context = context;
        _aiSettingsService = aiSettingsService;
    }

    // GET: api/UserAiSettings
    [HttpGet]
    [ProducesResponseType(typeof(AiGenerationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiGenerationSettingsResponse?>> GetUserAiSettings(CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        var user = await _context.Users
            .Include(u => u.AiSettings)
            .FirstOrDefaultAsync(u => u.Id == appUserId.Value, cancellationToken);

        if (user?.AiSettings == null)
        {
            return Ok(null); // User has no AI settings configured
        }

        var response = new AiGenerationSettingsResponse(
            user.AiSettings.Id,
            user.AiSettings.SettingsType,
            user.AiSettings.ChatGenerationProvider,
            user.AiSettings.ChatGenerationModel,
            user.AiSettings.ImagePromptGenerationProvider,
            user.AiSettings.ImagePromptGenerationModel,
            user.AiSettings.ImageGenerationProvider,
            user.AiSettings.ImageGenerationModel,
            user.AiSettings.ImageGenerationPromptInstruction
        );

        return Ok(response);
    }

    // PUT: api/UserAiSettings
    [HttpPut]
    [ProducesResponseType(typeof(AiGenerationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiGenerationSettingsResponse>> UpdateUserAiSettings(
        [FromBody] AiGenerationSettingsRequest request, 
        CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        var user = await _context.Users
            .Include(u => u.AiSettings)
            .FirstOrDefaultAsync(u => u.Id == appUserId.Value, cancellationToken);

        if (user == null)
        {
            return BadRequest("User not found.");
        }

        AiGenerationSettings aiSettings;

        if (user.AiSettings != null)
        {
            // Update existing settings
            aiSettings = user.AiSettings;
            aiSettings.ChatGenerationProvider = request.ChatGenerationProvider;
            aiSettings.ChatGenerationModel = request.ChatGenerationModel;
            aiSettings.ImagePromptGenerationProvider = request.ImagePromptGenerationProvider;
            aiSettings.ImagePromptGenerationModel = request.ImagePromptGenerationModel;
            aiSettings.ImageGenerationProvider = request.ImageGenerationProvider;
            aiSettings.ImageGenerationModel = request.ImageGenerationModel;
            aiSettings.ImageGenerationPromptInstruction = request.ImageGenerationPromptInstruction;
            aiSettings.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new settings
            aiSettings = new AiGenerationSettings
            {
                SettingsType = "User",
                ChatGenerationProvider = request.ChatGenerationProvider,
                ChatGenerationModel = request.ChatGenerationModel,
                ImagePromptGenerationProvider = request.ImagePromptGenerationProvider,
                ImagePromptGenerationModel = request.ImagePromptGenerationModel,
                ImageGenerationProvider = request.ImageGenerationProvider,
                ImageGenerationModel = request.ImageGenerationModel,
                ImageGenerationPromptInstruction = request.ImageGenerationPromptInstruction
            };

            user.AiSettings = aiSettings;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new AiGenerationSettingsResponse(
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

        return Ok(response);
    }

    // DELETE: api/UserAiSettings
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserAiSettings(CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        var user = await _context.Users
            .Include(u => u.AiSettings)
            .FirstOrDefaultAsync(u => u.Id == appUserId.Value, cancellationToken);

        if (user?.AiSettings == null)
        {
            return NotFound("User AI settings not found.");
        }

        // Remove the AI settings reference from user
        var settingsId = user.AiSettings.Id;
        user.AiSettings = null;
        user.AiSettingsId = null;

        await _context.SaveChangesAsync(cancellationToken);

        // Delete the AI settings record
        await _aiSettingsService.DeleteSettingsAsync(settingsId);

        return NoContent();
    }
}