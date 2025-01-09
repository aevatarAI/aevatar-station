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
    
    public async Task<BaseStateIndex> QueryAsync(string index, string id)
    {
        //return await _cqrsProvider.QueryStateAsync(index, id);
        return null;
    }

    public async Task SendEventCommandAsync(EventBase eventBase)
    {
        await _cqrsProvider.SendEventCommandAsync(eventBase);
    }

    public async Task<K> QueryGEventAsync<T, K>(string index, string id) where T : GEventBase
    {
        /*try
        {
            var documentContent = await _cqrsProvider.QueryGEventAsync(index, id);
            var gEvent = JsonConvert.DeserializeObject<T>(documentContent);
            return _objectMapper.Map<T, K>(gEvent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "QueryGEventAsync error JsonException index:{index} id:{id}", index, id);
            return default(K);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "QueryGEventAsync error index:{index} id:{id}", index, id);
            throw;
        }*/
        throw new Exception();
    }
}