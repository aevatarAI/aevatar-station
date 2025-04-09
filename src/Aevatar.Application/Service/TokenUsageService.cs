using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Aevatar.TokenUsage;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

public class TokenUsageService : ITokenUsageService, ITransientDependency
{
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IOptions<HostOptions> _hostOptions;
    private readonly ILogger<TokenUsageService> _logger;

    public TokenUsageService(ICQRSProvider cqrsProvider, ILogger<TokenUsageService> logger,
        IOptions<HostOptions> hostOptions)
    {
        _cqrsProvider = cqrsProvider;
        _logger = logger;
        _hostOptions = hostOptions;
    }

    public async Task<List<TokenUsageResponseDto>> GetTokenUsageAsync(TokenUsageRequestDto requestDto)
    {
        var result = new List<TokenUsageResponseDto>();

        var response = await _cqrsProvider.QueryTokenUsage(GetHostId(requestDto.ProjectId), requestDto.SystemLLM,
            requestDto.StartTime,
            requestDto.EndTime, requestDto.StatisticsAsHour, requestDto.Count);
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
            result.Add(JsonConvert.DeserializeObject<TokenUsageResponseDto>(item)!);
        }

        return result;
    }

    private string GetHostId(Guid projectId)
    {
        // todo: this will change to projectId
        return _hostOptions.Value.HostId;
    }
}