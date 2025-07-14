using System.ComponentModel.DataAnnotations;

namespace Aevatar.Anonymous;

/// <summary>
/// Request DTO for guest chat
/// </summary>
public class GuestChatRequestDto
{
    /// <summary>
    /// Chat message content
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional region identifier
    /// </summary>
    [MaxLength(10)]
    public string? Region { get; set; }
} 