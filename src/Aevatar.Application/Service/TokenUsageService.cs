using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.CQRS.Provider;
using Aevatar.TokenUsage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

public class TokenUsageService:ITokenUsageService,ISingletonDependency
{
    private readonly ICQRSProvider _cqrsProvider;
    private readonly Logger<TokenUsageService> _logger;

    public TokenUsageService(ICQRSProvider cqrsProvider, Logger<TokenUsageService> logger)
    {
        _cqrsProvider = cqrsProvider;
        _logger = logger;
    }

    public async Task<List<TokenUsageResponseDto>> GetTokenUsageAsync(TokenUsageRequestDto requestDto)
    {
        var result = new List<TokenUsageResponseDto>();
        
        var response = await _cqrsProvider.QueryTokenUsage(requestDto.ProjectId, requestDto.SystemLLM, requestDto.StartTime,
            requestDto.EndTime, requestDto.StatisticsAsHour);
        if (response == null)
        {
            _logger.LogError("[TokenUsageService] GetTokenUsageAsync null");
            return new List<TokenUsageResponseDto>();
        }

        if (response.Item1 == 0)
        {
            return result;
        }
        
        foreach (var item in response.Item2)
        {
            result.Add( JsonConvert.DeserializeObject<TokenUsageResponseDto>(item)!);
        }

        return result;
    }
}