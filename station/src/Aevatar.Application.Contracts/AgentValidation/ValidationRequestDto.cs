using System.ComponentModel.DataAnnotations;

namespace Aevatar.AgentValidation;

/// <summary>
/// Agent configuration validation request DTO
/// </summary>
public class ValidationRequestDto
{
    public string GAgentNamespace { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
}