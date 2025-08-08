
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserSettingsController : BaseApiController
    {
        private readonly IUserSettingsService _userSettingsService;

        public UserSettingsController(IUserService userService, IUserSettingsService userSettingsService, ILogger<UserSettingsController> logger)
            : base(userService, logger)
        {
            _userSettingsService = userSettingsService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserSettingDto>>> GetUserSettings(CancellationToken cancellationToken)
        {
            var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
            if (errorResult != null) return errorResult;

            var settings = await _userSettingsService.GetUserSettingsAsync(userId!.Value);
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserSettings([FromBody] List<UserSettingDto> settings, CancellationToken cancellationToken)
        {
            var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
            if (errorResult != null) return errorResult;

            var success = await _userSettingsService.UpdateUserSettingsAsync(userId!.Value, settings);
            if (success)
            {
                return NoContent();
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update user settings.");
            }
        }
    }
}
