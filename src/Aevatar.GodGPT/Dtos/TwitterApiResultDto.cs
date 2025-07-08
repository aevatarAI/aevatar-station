namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Generic API result wrapper for Twitter operations containing success status, error information and data
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class TwitterApiResultDto<T>
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message when operation fails, null when successful
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// The actual data returned by the operation
    /// </summary>
    public T Data { get; set; }
} 