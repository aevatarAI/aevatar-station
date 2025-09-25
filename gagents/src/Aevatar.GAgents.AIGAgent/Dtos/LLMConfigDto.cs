using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

/// <summary>
/// Simplified LLM configuration DTO with direct Provider + Model separation support.
/// This is the new recommended approach for AI configuration.
/// </summary>
[GenerateSerializer]
public class LLMConfigDto
{
    // ===== NEW SEPARATED CONFIGURATION (RECOMMENDED) =====
    
    [Id(0)]
    [Description("LLM Provider type (Azure, OpenAI, DeepSeek, Google, BytePlus)")]
    public LLMProviderEnum? ProviderType { get; set; } = null;
    
    [Id(1)]
    [Description("LLM Model type (OpenAI, DeepSeek, Gemini, etc.)")]  
    public ModelIdEnum? ModelType { get; set; } = null;
    
    // ===== BACKWARD COMPATIBILITY FIELDS =====
    
    [Id(2)]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "System LLM must be between 1 and 100 characters")]
    [Description("Legacy system-level LLM name (for backward compatibility)")]
    public string? SystemLLM { get; set; } = null;
    
    /// <summary>
    /// Resolves LLM service using the configuration service (dynamic configuration)
    /// This method delegates to ILLMConfigurationService for intelligent resolution
    /// Uses ConfigurationContext to abstract business-level identifiers
    /// </summary>
    public async Task<LLMService> ResolveLLMServiceAsync(
        ILLMConfigurationService configurationService,
        ConfigurationContext? context = null)
    {
        // Priority 1: Use new separated Provider + Model types (RECOMMENDED)
        if (ProviderType.HasValue && ModelType.HasValue)
        {
            return await configurationService.ResolveLLMServiceAsync(
                ProviderType.Value, ModelType.Value, context);
        }
        
        // Priority 2: Use SystemLLM for backward compatibility
        if (!string.IsNullOrWhiteSpace(SystemLLM))
        {
            return await configurationService.ResolveLLMServiceFromSystemLLMAsync(
                SystemLLM, context);
        }
        
        // Fallback: Default to OpenAI
        return await configurationService.ResolveLLMServiceAsync(
            LLMProviderEnum.OpenAI, ModelIdEnum.OpenAI, context);
    }
    
    /// <summary>
    /// Validates that the configuration has either ProviderType+ModelType or SystemLLM
    /// </summary>
    public bool IsValid()
    {
        // Valid if we have both ProviderType and ModelType
        if (ProviderType.HasValue && ModelType.HasValue)
        {
            return true;
        }
        
        // Valid if we have SystemLLM for backward compatibility
        if (!string.IsNullOrEmpty(SystemLLM))
        {
            return true;
        }
        
        return false;
    }
}