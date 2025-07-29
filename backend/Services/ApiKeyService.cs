using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AiRoleplayChat.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyService> _logger;
    private readonly string _defaultKeyVaultUri;

    public ApiKeyService(AppDbContext context, IConfiguration configuration, ILogger<ApiKeyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultKeyVaultUri = _configuration["KeyVault:VaultUri"] ?? throw new InvalidOperationException("Configuration missing: KeyVault:VaultUri");
    }

    public async Task<bool> StoreApiKeyAsync(int userId, string serviceName, string apiKey)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // ユーザー専用のKey Vault URIを使用、なければデフォルトを使用
            var keyVaultUri = user.KeyVaultUri ?? _defaultKeyVaultUri;
            var client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            // シークレット名: user-{userId}-{serviceName}-apikey
            var secretName = $"user-{userId}-{serviceName.ToLowerInvariant()}-apikey";
            
            await client.SetSecretAsync(secretName, apiKey);
            
            _logger.LogInformation("API key stored successfully for user {UserId}, service {ServiceName}", userId, serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store API key for user {UserId}, service {ServiceName}", userId, serviceName);
            return false;
        }
    }

    public async Task<string?> GetApiKeyAsync(int userId, string serviceName)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            // ユーザー専用のKey Vault URIを使用、なければデフォルトを使用
            var keyVaultUri = user.KeyVaultUri ?? _defaultKeyVaultUri;
            var client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            // シークレット名: user-{userId}-{serviceName}-apikey
            var secretName = $"user-{userId}-{serviceName.ToLowerInvariant()}-apikey";
            
            var response = await client.GetSecretAsync(secretName);
            return response.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("API key not found for user {UserId}, service {ServiceName}", userId, serviceName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API key for user {UserId}, service {ServiceName}", userId, serviceName);
            return null;
        }
    }

    public async Task<bool> DeleteApiKeyAsync(int userId, string serviceName)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // ユーザー専用のKey Vault URIを使用、なければデフォルトを使用
            var keyVaultUri = user.KeyVaultUri ?? _defaultKeyVaultUri;
            var client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            // シークレット名: user-{userId}-{serviceName}-apikey
            var secretName = $"user-{userId}-{serviceName.ToLowerInvariant()}-apikey";
            
            var operation = await client.StartDeleteSecretAsync(secretName);
            await operation.WaitForCompletionAsync();
            
            _logger.LogInformation("API key deleted successfully for user {UserId}, service {ServiceName}", userId, serviceName);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("API key not found for deletion for user {UserId}, service {ServiceName}", userId, serviceName);
            return true; // 存在しないものは削除済みとみなす
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key for user {UserId}, service {ServiceName}", userId, serviceName);
            return false;
        }
    }

    public async Task<bool> SetUserKeyVaultUriAsync(int userId, string keyVaultUri)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            user.KeyVaultUri = keyVaultUri;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Key Vault URI set successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Key Vault URI for user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserRegisteredServicesAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new List<string>();
            }

            // ユーザー専用のKey Vault URIを使用、なければデフォルトを使用
            var keyVaultUri = user.KeyVaultUri ?? _defaultKeyVaultUri;
            var client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            var services = new List<string>();
            var prefix = $"user-{userId}-";
            var suffix = "-apikey";

            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                if (secretProperties.Name.StartsWith(prefix) && secretProperties.Name.EndsWith(suffix))
                {
                    // user-{userId}-{serviceName}-apikey から {serviceName} を抽出
                    var serviceName = secretProperties.Name
                        .Substring(prefix.Length, secretProperties.Name.Length - prefix.Length - suffix.Length);
                    services.Add(serviceName);
                }
            }

            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registered services for user {UserId}", userId);
            return new List<string>();
        }
    }
}