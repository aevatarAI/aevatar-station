using System.ComponentModel.DataAnnotations;

namespace Aevatar.AgentValidation;

/// <summary>
/// Agent configuration validation request DTO
/// </summary>
public class ValidationRequestDto
{
    /// <summary>
    /// Required: Complete GAgent Namespace (e.g., "Aevatar.GAgents.DatabaseGAgent")
    /// </summary>
    [Required(ErrorMessage = "GAgent Namespace is required")]
    public string GAgentNamespace { get; set; } = string.Empty;
    
    /// <summary>
    /// Required: Configuration JSON string
    /// </summary>
    [Required(ErrorMessage = "Configuration JSON is required")]
    public string ConfigJson { get; set; } = string.Empty;
}