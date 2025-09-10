namespace Aevatar.AgentValidation;

/// <summary>
/// Validation error details
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Property name that caused the error
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message description
    /// </summary>
    public string Message { get; set; } = string.Empty;
}