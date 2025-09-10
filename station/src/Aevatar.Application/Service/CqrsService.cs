using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Query;
using Newtonsoft.Json;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Service;

public class CqrsService : ApplicationService, ICqrsService
{
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CqrsService> _logger;

    public CqrsService(ICQRSProvider cqrsProvider, IObjectMapper objectMapper, ILogger<CqrsService> logger)
    {
        _cqrsProvider = cqrsProvider;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<AgentStateDto> QueryStateAsync(string stateName, Guid guid)
    {
        stateName = stateName.ToLower();
        var stopwatch = Stopwatch.StartNew();
        var data = await _cqrsProvider.QueryAgentStateAsync(stateName, guid);
        stopwatch.Stop();

        _logger.LogInformation("QueryStateAsync, index: {stateName}, guid: {guid}, cost: {time}", stateName, guid,
            stopwatch.ElapsedMilliseconds);
        if (data.IsNullOrEmpty())
        {
            _logger.LogError("state not exist for name: {name} and guid: {guid}", stateName, guid);
            throw new UserFriendlyException("state not exist");
        }

        return new AgentStateDto
        {
            State = JsonConvert.DeserializeObject<Dictionary<string, object>>(data)
        };
    }
}