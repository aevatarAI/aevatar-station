using System.ComponentModel.DataAnnotations;

namespace Aevatar.Anonymous;

/// <summary>
/// Request DTO for creating guest session
/// </summary>
public class CreateGuestSessionRequestDto
{
    /// <summary>
    /// Optional guider/role for the session (e.g., "assistant", "teacher")
    /// </summary>
    [MaxLength(50)]
    public string? Guider { get; set; }
    public string Ip { get; set; }
    
} 