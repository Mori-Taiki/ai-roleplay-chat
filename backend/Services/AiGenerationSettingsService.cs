using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Services
{
    public interface IAiGenerationSettingsService
    {
        Task<AiGenerationSettings?> GetSettingsAsync(int settingsId);
        Task<AiGenerationSettings> CreateOrUpdateSettingsAsync(AiGenerationSettings settings);
        Task<bool> DeleteSettingsAsync(int settingsId);
    }

    public class AiGenerationSettingsService : IAiGenerationSettingsService
    {
        private readonly AppDbContext _context;

        public AiGenerationSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AiGenerationSettings?> GetSettingsAsync(int settingsId)
        {
            return await _context.AiGenerationSettings
                .FirstOrDefaultAsync(s => s.Id == settingsId);
        }

        public async Task<AiGenerationSettings> CreateOrUpdateSettingsAsync(AiGenerationSettings settings)
        {
            if (settings.Id == 0)
            {
                // Create new settings
                settings.CreatedAt = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                _context.AiGenerationSettings.Add(settings);
            }
            else
            {
                // Update existing settings
                var existing = await _context.AiGenerationSettings
                    .FirstOrDefaultAsync(s => s.Id == settings.Id);
                
                if (existing == null)
                {
                    throw new ArgumentException($"AI Generation Settings with ID {settings.Id} not found");
                }

                existing.ChatGenerationProvider = settings.ChatGenerationProvider;
                existing.ChatGenerationModel = settings.ChatGenerationModel;
                existing.ImagePromptGenerationProvider = settings.ImagePromptGenerationProvider;
                existing.ImagePromptGenerationModel = settings.ImagePromptGenerationModel;
                existing.ImageGenerationProvider = settings.ImageGenerationProvider;
                existing.ImageGenerationModel = settings.ImageGenerationModel;
                existing.ImageGenerationPromptInstruction = settings.ImageGenerationPromptInstruction;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task<bool> DeleteSettingsAsync(int settingsId)
        {
            var settings = await _context.AiGenerationSettings
                .FirstOrDefaultAsync(s => s.Id == settingsId);
                
            if (settings == null)
                return false;

            _context.AiGenerationSettings.Remove(settings);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}