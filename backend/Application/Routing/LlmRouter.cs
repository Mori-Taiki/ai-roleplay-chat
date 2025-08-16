using AiRoleplayChat.Backend.Application.Ports;
using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Options;
using AiRoleplayChat.Backend.Services;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

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
    private readonly IAiGenerationSettingsService _aiSettingsService;
    private readonly AppDbContext _context;
    private readonly ProviderOptions _providerOptions;

    public LlmRouter(
        IServiceProvider serviceProvider,
        IUserSettingsService userSettingsService,
        IAiGenerationSettingsService aiSettingsService,
        AppDbContext context,
        IOptions<ProviderOptions> providerOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        _aiSettingsService = aiSettingsService ?? throw new ArgumentNullException(nameof(aiSettingsService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _providerOptions = providerOptions?.Value ?? throw new ArgumentNullException(nameof(providerOptions));
    }

    /// <summary>
    /// Resolve text model with priority: Character > User > Default
    /// </summary>
    public async Task<ITextModelPort> ResolveTextModelAsync(CharacterProfile? characterProfile, int? userId)
    {
        string? textModel = null;

        // Priority 1: Character-specific AI settings
        if (characterProfile?.AiSettingsId.HasValue == true)
        {
            var characterAiSettings = await _aiSettingsService.GetSettingsAsync(characterProfile.AiSettingsId.Value);
            textModel = characterAiSettings?.ChatGenerationModel;
        }

        // Priority 2: User-specific AI settings
        if (string.IsNullOrEmpty(textModel) && userId.HasValue)
        {
            var user = await _context.Users
                .Include(u => u.AiSettings)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);
            
            textModel = user?.AiSettings?.ChatGenerationModel;
        }

        // Priority 3: Default from configuration
        if (string.IsNullOrEmpty(textModel))
        {
            textModel = _providerOptions.Default.TextProvider;
        }

        // Resolve provider name to service (extract provider from full model name)
        string providerName = ExtractProviderFromModel(textModel);
        return ResolveTextProvider(providerName);
    }

    /// <summary>
    /// Resolve image model with priority: Character > User > Default
    /// </summary>
    public async Task<IImageModelPort> ResolveImageModelAsync(CharacterProfile? characterProfile, int? userId)
    {
        string? imageModel = null;

        // Priority 1: Character-specific AI settings
        if (characterProfile?.AiSettingsId.HasValue == true)
        {
            var characterAiSettings = await _aiSettingsService.GetSettingsAsync(characterProfile.AiSettingsId.Value);
            imageModel = characterAiSettings?.ImageGenerationModel;
        }

        // Priority 2: User-specific AI settings
        if (string.IsNullOrEmpty(imageModel) && userId.HasValue)
        {
            var user = await _context.Users
                .Include(u => u.AiSettings)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);
            
            imageModel = user?.AiSettings?.ImageGenerationModel;
        }

        // Priority 3: Default from configuration
        if (string.IsNullOrEmpty(imageModel))
        {
            imageModel = _providerOptions.Default.ImageProvider;
        }

        // Resolve provider name to service (extract provider from full model name)
        string providerName = ExtractProviderFromModel(imageModel);
        return ResolveImageProvider(providerName);
    }

    /// <summary>
    /// Extract provider name from full model string
    /// e.g., "gemini-1.5-flash-latest" -> "gemini"
    /// e.g., "black-forest-labs/flux-1-dev" -> "replicate" 
    /// </summary>
    private string ExtractProviderFromModel(string? modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return _providerOptions.Default.TextProvider;

        // Common patterns for different providers
        if (modelName.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            return "Gemini";
        
        if (modelName.Contains("/") || modelName.StartsWith("black-forest-labs", StringComparison.OrdinalIgnoreCase))
            return "Replicate";
            
        if (modelName.StartsWith("gpt", StringComparison.OrdinalIgnoreCase) || 
            modelName.StartsWith("claude", StringComparison.OrdinalIgnoreCase))
            return "OpenAI"; // or appropriate provider

        // Default fallback - assume it's the provider name itself
        return modelName.Split('-', '/')[0];
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