
using AiRoleplayChat.Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Services
{
    public interface IUserSettingsService
    {
        Task<List<UserSettingDto>> GetUserSettingsAsync(int userId);
        Task<bool> UpdateUserSettingsAsync(int userId, List<UserSettingDto> settings);
    }
}
