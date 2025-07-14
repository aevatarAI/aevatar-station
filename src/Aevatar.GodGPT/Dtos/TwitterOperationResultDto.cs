namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Result of Twitter operation containing success status and error information
/// </summary>
public class TwitterOperationResultDto
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message when operation fails, null when successful
    /// </summary>
    public string? ErrorMessage { get; set; }
} 