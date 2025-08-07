using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiKeyController : BaseApiController
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyController(IUserService userService, IApiKeyService apiKeyService, ILogger<ApiKeyController> logger)
        : base(userService, logger)
    {
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
    }

    /// <summary>
    /// ユーザーのAPIキーを登録します
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> RegisterApiKey([FromBody] ApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        if (string.IsNullOrWhiteSpace(request.ServiceName) || string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest("ServiceName and ApiKey are required.");
        }

        var success = await _apiKeyService.StoreApiKeyAsync(userId!.Value, request.ServiceName, request.ApiKey);
        
        if (success)
        {
            _logger.LogInformation("API key registered successfully for user {UserId}, service {ServiceName}", userId, request.ServiceName);
            return Ok(new { Message = "API key registered successfully." });
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to register API key.");
        }
    }

    /// <summary>
    /// ユーザーのAPIキーの存在確認を行います（実際のキー値は返しません）
    /// </summary>
    [HttpGet("{serviceName}")]
    public async Task<ActionResult<ApiKeyResponse>> CheckApiKey(string serviceName, CancellationToken cancellationToken = default)
    {
        var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return BadRequest("ServiceName is required.");
        }

        var apiKey = await _apiKeyService.GetApiKeyAsync(userId!.Value, serviceName);
        
        return Ok(new ApiKeyResponse
        {
            ServiceName = serviceName,
            HasKey = !string.IsNullOrEmpty(apiKey)
        });
    }

    /// <summary>
    /// ユーザーのAPIキーを削除します
    /// </summary>
    [HttpDelete("{serviceName}")]
    public async Task<ActionResult> DeleteApiKey(string serviceName, CancellationToken cancellationToken = default)
    {
        var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return BadRequest("ServiceName is required.");
        }

        var success = await _apiKeyService.DeleteApiKeyAsync(userId!.Value, serviceName);
        
        if (success)
        {
            _logger.LogInformation("API key deleted successfully for user {UserId}, service {ServiceName}", userId, serviceName);
            return Ok(new { Message = "API key deleted successfully." });
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete API key.");
        }
    }

        /// <summary>
    /// ユーザーの登録済みサービス一覧を取得します
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserApiKeysResponse>> GetUserApiKeys(CancellationToken cancellationToken = default)
    {
        var (userId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        var services = await _apiKeyService.GetUserRegisteredServicesAsync(userId!.Value);
        
        return Ok(new UserApiKeysResponse
        {
            RegisteredServices = services,
        });
    }
}