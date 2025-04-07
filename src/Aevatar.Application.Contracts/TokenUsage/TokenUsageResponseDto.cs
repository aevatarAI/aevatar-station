using Google.Type;

namespace Aevatar.TokenUsage;

public class TokenUsageResponseDto
{
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public DateTime Time { get; set; }
}