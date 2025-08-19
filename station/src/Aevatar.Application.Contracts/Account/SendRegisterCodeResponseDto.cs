namespace Aevatar.Account;

/// <summary>
/// Response DTO for send register code operation
/// </summary>
public class SendRegisterCodeResponseDto
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = "";
}
