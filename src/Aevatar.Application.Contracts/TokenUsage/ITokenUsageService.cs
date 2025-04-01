using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.TokenUsage;

public interface ITokenUsageService
{
    Task<List<TokenUsageResponseDto>> GetTokenUsageAsync(TokenUsageRequestDto requestDto);
}