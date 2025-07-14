namespace Aevatar.Anonymous;

/// <summary>
/// Response DTO for create guest session
/// </summary>
public class CreateGuestSessionResponseDto
{
    public int RemainingChats { get; set; }
    public int TotalAllowed { get; set; }
}

/// <summary>
/// Response DTO for guest chat limits query
/// </summary>
public class GuestChatLimitsResponseDto
{
    public int RemainingChats { get; set; }
    public int TotalAllowed { get; set; }
} 