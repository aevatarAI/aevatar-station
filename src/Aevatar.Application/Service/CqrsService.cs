using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agents.Atomic;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Newtonsoft.Json;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using JsonException = System.Text.Json.JsonException;

namespace Aevatar.Service;

public class CqrsService : ApplicationService,ICqrsService
{
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CqrsService> _logger;

    public CqrsService(ICQRSProvider cqrsProvider,IObjectMapper objectMapper,ILogger<CqrsService> logger)
    {
        _cqrsProvider = cqrsProvider;
        _objectMapper = objectMapper;
        _logger = logger;

    }
    
    public async Task<Tuple<long, List<AgentGEventIndex>>> QueryGEventAsync(string eventId, List<string> groupIds, int pageNumber, int pageSize)
    {
        try
        {
            var resp = await _cqrsProvider.QueryGEventAsync(eventId, groupIds, pageNumber, pageSize);
            return resp;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryGEventAsync error eventId:{eventId} id:{groupIds} pageNumber:{pageNumber} pageSize:{pageSize}", eventId, JsonConvert.SerializeObject(groupIds), pageNumber, pageSize);
            throw;
        }
    }
}