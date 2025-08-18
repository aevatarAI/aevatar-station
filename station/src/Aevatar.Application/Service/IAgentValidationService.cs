using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AgentValidation;

namespace Aevatar.Service;

/// <summary>
/// Agent configuration validation service interface
/// </summary>
public interface IAgentValidationService
{
    /// <summary>
    /// Validate agent configuration based on GAgent namespace and JSON configuration
    /// </summary>
    /// <param name="request">Validation request containing GAgent namespace and config JSON</param>
    /// <returns>Validation result with success status and error details</returns>
    Task<ConfigValidationResultDto> ValidateConfigAsync(ValidationRequestDto request);
    
    /// <summary>
    /// Get configuration schema for the specified GAgent type
    /// </summary>
    /// <param name="gAgentNamespace">Complete GAgent namespace</param>
    /// <returns>JSON schema string for the configuration type, or null if not found</returns>
    Task<string?> GetConfigurationSchemaAsync(string gAgentNamespace);
}