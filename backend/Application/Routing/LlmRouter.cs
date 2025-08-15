using AiRoleplayChat.Backend.Application.Ports;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Options;
using AiRoleplayChat.Backend.Services;
using Microsoft.Extensions.Options;

namespace AiRoleplayChat.Backend.Application.Routing;

/// <summary>
/// Router interface for resolving text and image models based on priority:
/// Character settings > User settings > Default settings
/// </summary>
public interface ILlmRouter
{
    /// <summary>
    /// Resolve text model provider with priority: Character > User > Default
    /// </summary>
    /// <param name="characterProfile">Character profile with potential model settings</param>
    /// <param name="userId">User ID for user-specific settings</param>
    /// <returns>Text model port instance</returns>
    Task<ITextModelPort> ResolveTextModelAsync(CharacterProfile? characterProfile, int? userId);

    /// <summary>
    /// Resolve image model provider with priority: Character > User > Default
    /// </summary>
    /// <param name="characterProfile">Character profile with potential model settings</param>
    /// <param name="userId">User ID for user-specific settings</param>
    /// <returns>Image model port instance</returns>
    Task<IImageModelPort> ResolveImageModelAsync(CharacterProfile? characterProfile, int? userId);
}

/// <summary>
/// Implementation of LLM router that resolves models based on priority
/// </summary>
public class LlmRouter : ILlmRouter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ProviderOptions _providerOptions;

    public LlmRouter(
        IServiceProvider serviceProvider,
        IUserSettingsService userSettingsService,
        IOptions<ProviderOptions> providerOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        _providerOptions = providerOptions?.Value ?? throw new ArgumentNullException(nameof(providerOptions));
    }

    /// <summary>
    /// Resolve text model with priority: Character > User > Default
    /// </summary>
    public async Task<ITextModelPort> ResolveTextModelAsync(CharacterProfile? characterProfile, int? userId)
    {
        string textProvider = _providerOptions.Default.TextProvider;

        // Priority 1: Character-specific text model
        if (characterProfile != null && !string.IsNullOrEmpty(characterProfile.TextModelProvider))
        {
            textProvider = characterProfile.TextModelProvider;
        }
        // Priority 2: User-specific text model
        else if (userId.HasValue)
        {
            var userSettings = await _userSettingsService.GetUserSettingsAsync(userId.Value);
            var textProviderSetting = userSettings.FirstOrDefault(s => 
                s.SettingKey == "DefaultTextProvider" && !string.IsNullOrEmpty(s.SettingValue));
            
            if (textProviderSetting != null)
            {
                textProvider = textProviderSetting.SettingValue!;
            }
        }

        // Resolve provider to service
        return ResolveTextProvider(textProvider);
    }

    /// <summary>
    /// Resolve image model with priority: Character > User > Default
    /// </summary>
    public async Task<IImageModelPort> ResolveImageModelAsync(CharacterProfile? characterProfile, int? userId)
    {
        string imageProvider = _providerOptions.Default.ImageProvider;

        // Priority 1: Character-specific image model
        if (characterProfile != null && !string.IsNullOrEmpty(characterProfile.ImageModelProvider))
        {
            imageProvider = characterProfile.ImageModelProvider;
        }
        // Priority 2: User-specific image model
        else if (userId.HasValue)
        {
            var userSettings = await _userSettingsService.GetUserSettingsAsync(userId.Value);
            var imageProviderSetting = userSettings.FirstOrDefault(s => 
                s.SettingKey == "DefaultImageProvider" && !string.IsNullOrEmpty(s.SettingValue));
            
            if (imageProviderSetting != null)
            {
                imageProvider = imageProviderSetting.SettingValue!;
            }
        }

        // Resolve provider to service
        return ResolveImageProvider(imageProvider);
    }

    private ITextModelPort ResolveTextProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "gemini" => _serviceProvider.GetRequiredService<ITextModelPort>(),
            _ => throw new NotSupportedException($"Text provider '{providerName}' is not supported")
        };
    }

    private IImageModelPort ResolveImageProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "replicate" => _serviceProvider.GetRequiredService<IImageModelPort>(),
            _ => throw new NotSupportedException($"Image provider '{providerName}' is not supported")
        };
    }
}