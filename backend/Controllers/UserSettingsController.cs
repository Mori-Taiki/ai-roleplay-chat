
using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserSettingsController : BaseApiController
    {
        private readonly AppDbContext _context;

        public UserSettingsController(IUserService userService, AppDbContext context, ILogger<UserSettingsController> logger)
            : base(userService, logger)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserSettingDto>>> GetUserSettings(CancellationToken cancellationToken)
        {
            var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
            if (errorResult != null) return errorResult;

            var user = await _context.Users
                .Include(u => u.AiSettings)
                .FirstOrDefaultAsync(u => u.Id == userId!.Value, cancellationToken);

            var settings = new List<UserSettingDto>();

            if (user?.AiSettings != null)
            {
                // Convert AiGenerationSettings back to UserSetting format for backward compatibility
                var aiSettings = user.AiSettings;

                if (!string.IsNullOrEmpty(aiSettings.ChatGenerationModel))
                {
                    settings.Add(new UserSettingDto
                    {
                        ServiceType = "Gemini",
                        SettingKey = "ChatModel",
                        SettingValue = aiSettings.ChatGenerationModel
                    });
                }

                if (!string.IsNullOrEmpty(aiSettings.ImagePromptGenerationModel))
                {
                    settings.Add(new UserSettingDto
                    {
                        ServiceType = "Gemini",
                        SettingKey = "ImagePromptGenerationModel",
                        SettingValue = aiSettings.ImagePromptGenerationModel
                    });
                }

                if (!string.IsNullOrEmpty(aiSettings.ImageGenerationModel))
                {
                    settings.Add(new UserSettingDto
                    {
                        ServiceType = "Replicate",
                        SettingKey = "ImageGenerationVersion",
                        SettingValue = aiSettings.ImageGenerationModel
                    });
                }

                if (!string.IsNullOrEmpty(aiSettings.ImageGenerationPromptInstruction))
                {
                    settings.Add(new UserSettingDto
                    {
                        ServiceType = "Gemini",
                        SettingKey = "ImagePromptInstruction",
                        SettingValue = aiSettings.ImageGenerationPromptInstruction
                    });
                }
            }

            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserSettings([FromBody] List<UserSettingDto> settings, CancellationToken cancellationToken)
        {
            var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
            if (errorResult != null) return errorResult;

            var user = await _context.Users
                .Include(u => u.AiSettings)
                .FirstOrDefaultAsync(u => u.Id == userId!.Value, cancellationToken);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Convert UserSetting format back to AiGenerationSettings
            string? chatModel = null;
            string? imagePromptModel = null;
            string? imageModel = null;
            string? promptInstruction = null;

            foreach (var setting in settings)
            {
                if (setting.ServiceType == "Gemini" && setting.SettingKey == "ChatModel")
                    chatModel = setting.SettingValue;
                else if (setting.ServiceType == "Gemini" && setting.SettingKey == "ImagePromptGenerationModel")
                    imagePromptModel = setting.SettingValue;
                else if (setting.ServiceType == "Replicate" && setting.SettingKey == "ImageGenerationVersion")
                    imageModel = setting.SettingValue;
                else if (setting.ServiceType == "Gemini" && setting.SettingKey == "ImagePromptInstruction")
                    promptInstruction = setting.SettingValue;
            }

            AiGenerationSettings aiSettings;

            if (user.AiSettings != null)
            {
                // Update existing settings
                aiSettings = user.AiSettings;
                aiSettings.ChatGenerationProvider = !string.IsNullOrEmpty(chatModel) ? "Gemini" : null;
                aiSettings.ChatGenerationModel = chatModel;
                aiSettings.ImagePromptGenerationProvider = !string.IsNullOrEmpty(imagePromptModel) ? "Gemini" : null;
                aiSettings.ImagePromptGenerationModel = imagePromptModel;
                aiSettings.ImageGenerationProvider = !string.IsNullOrEmpty(imageModel) ? "Replicate" : null;
                aiSettings.ImageGenerationModel = imageModel;
                aiSettings.ImageGenerationPromptInstruction = promptInstruction;
                aiSettings.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new settings
                aiSettings = new AiGenerationSettings
                {
                    SettingsType = "User",
                    ChatGenerationProvider = !string.IsNullOrEmpty(chatModel) ? "Gemini" : null,
                    ChatGenerationModel = chatModel,
                    ImagePromptGenerationProvider = !string.IsNullOrEmpty(imagePromptModel) ? "Gemini" : null,
                    ImagePromptGenerationModel = imagePromptModel,
                    ImageGenerationProvider = !string.IsNullOrEmpty(imageModel) ? "Replicate" : null,
                    ImageGenerationModel = imageModel,
                    ImageGenerationPromptInstruction = promptInstruction
                };

                user.AiSettings = aiSettings;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
}
