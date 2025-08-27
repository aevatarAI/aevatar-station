using System.Collections.Generic;
using System.Linq;

namespace Aevatar.AgentValidation;

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigValidationResultDto
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Overall validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static ConfigValidationResultDto Success(string message = "Validation passed")
    {
        return new ConfigValidationResultDto
        {
            IsValid = true,
            Message = message
        };
    }
    
    /// <summary>
    /// Create a failed validation result
    /// </summary>
    public static ConfigValidationResultDto Failure(string message = "Data validation failed")
    {
        return new ConfigValidationResultDto
        {
            IsValid = false,
            Message = message
        };
    }
}