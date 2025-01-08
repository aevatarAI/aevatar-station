using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Domain.Grains.Subscription;
using Aevatar.Subscription;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace Aevatar.Service;

public interface ISubscriptionAppService
{
    Task<List<EventTypeDto>> GetAvailableEventsAsync(string agentId);
    Task<SubscriptionDto> SubscribeAsync(CreateSubscriptionDto createSubscriptionDto);
    Task CancelSubscriptionAsync(Guid subscriptionId);
    Task<SubscriptionDto> GetSubscriptionAsync(Guid subscriptionId);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class SubscriptionAppService : ApplicationService, ISubscriptionAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<SubscriptionAppService> _logger;
    private readonly IObjectMapper _objectMapper;
    
    public SubscriptionAppService(
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        ILogger<SubscriptionAppService> logger)
    {
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    public async Task<List<EventTypeDto>> GetAvailableEventsAsync(string agentId)
    {
        return new List<EventTypeDto>(){};
    }

    public async Task<SubscriptionDto> SubscribeAsync(CreateSubscriptionDto createSubscriptionDto)
    {

      var  input = _objectMapper.Map<CreateSubscriptionDto, SubscribeEventInputDto>(createSubscriptionDto);
      var  subscriptionState = await _clusterClient.GetGrain<ISubscriptionGAgent>(GuidUtil.StringToGuid(createSubscriptionDto.AgentId))
            .SubscribeAsync(input);
      return _objectMapper.Map<EventSubscriptionState, SubscriptionDto>(subscriptionState);
    }

    public async Task CancelSubscriptionAsync(Guid subscriptionId)
    {
        await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId).UnsubscribeAsync();
    }

    public async Task<SubscriptionDto> GetSubscriptionAsync(Guid subscriptionId)
    {
        var subscriptionState = await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId)
            .GetStateAsync();
        return _objectMapper.Map<EventSubscriptionState, SubscriptionDto>(subscriptionState);
    }
}