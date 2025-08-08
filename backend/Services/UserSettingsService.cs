
using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly AppDbContext _context;

        public UserSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserSettingDto>> GetUserSettingsAsync(int userId)
        {
            return await _context.UserSettings
                .Where(s => s.UserId == userId)
                .Select(s => new UserSettingDto
                {
                    ServiceType = s.ServiceType,
                    SettingKey = s.SettingKey,
                    SettingValue = s.SettingValue
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateUserSettingsAsync(int userId, List<UserSettingDto> settings)
        {
            var existingSettings = await _context.UserSettings
                .Where(s => s.UserId == userId)
                .ToListAsync();

            foreach (var settingDto in settings)
            {
                var existingSetting = existingSettings.FirstOrDefault(s =>
                    s.ServiceType == settingDto.ServiceType && s.SettingKey == settingDto.SettingKey);

                if (existingSetting != null)
                {
                    existingSetting.SettingValue = settingDto.SettingValue;
                }
                else
                {
                    _context.UserSettings.Add(new UserSetting
                    {
                        UserId = userId,
                        ServiceType = settingDto.ServiceType,
                        SettingKey = settingDto.SettingKey,
                        SettingValue = settingDto.SettingValue
                    });
                }
            }
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                // エラーログを出力
                Console.Error.WriteLine($"Error updating user settings for user {userId}: {ex.Message}");
                return false;
            }
        }
    }
}
