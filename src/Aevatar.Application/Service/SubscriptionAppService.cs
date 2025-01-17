using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Combination;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.Domain.Grains.Subscription;
using Aevatar.Subscription;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        
        var list = await _clusterClient.GetGrain<IGAgent>(GuidUtil.StringToGuid(agentId)).GetAllSubscribedEventsAsync();
        return ConverEventTypeDtos(list);
    }
    
    static List<EventTypeDto> ConverEventTypeDtos(List<Type>? typeList)
    {
        var eventTypeList = new List<EventTypeDto>();
        if (typeList == null)
        {
            return eventTypeList;
        }
        foreach (var type in typeList)
        {
            var classDescription = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description available";

            var payload = new Dictionary<string, string>();
            foreach (var property in type.GetProperties())
            {
                var propertyDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name;
                var propertyType = property.PropertyType.Name; 
                payload[property.Name] = propertyType.ToLower(); 
            }
            
            var eventType = new EventTypeDto()
            {
                EventType = type.Name,
                Description = classDescription,
                Payload = payload
            };
            eventTypeList.Add(eventType);
        }
        return eventTypeList;
    }


    public async Task<SubscriptionDto> SubscribeAsync(CreateSubscriptionDto createSubscriptionDto)
    {

      var  input = _objectMapper.Map<CreateSubscriptionDto, SubscribeEventInputDto>(createSubscriptionDto);
      var subscriptionStateAgent =
          _clusterClient.GetGrain<ISubscriptionGAgent>(GuidUtil.StringToGuid(createSubscriptionDto.AgentId));
      var subscriptionState = await subscriptionStateAgent.SubscribeAsync(input);
      
      var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(ParseGuid(input.AgentId));
      await combinationAgent.RegisterAsync(subscriptionStateAgent);
      return _objectMapper.Map<EventSubscriptionState, SubscriptionDto>(subscriptionState);
    }

    public async Task CancelSubscriptionAsync(Guid subscriptionId)
    {
        var subscriptionStateAgent =
            _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId);
        var subscriptionState = await subscriptionStateAgent.GetStateAsync();
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(ParseGuid(subscriptionState.AgentId));
        await combinationAgent.UnregisterAsync(subscriptionStateAgent);
        await subscriptionStateAgent.UnsubscribeAsync();
    }

    public async Task<SubscriptionDto> GetSubscriptionAsync(Guid subscriptionId)
    {
        var subscriptionState = await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId)
            .GetStateAsync();
        return _objectMapper.Map<EventSubscriptionState, SubscriptionDto>(subscriptionState);
    }
    
    private Guid ParseGuid(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        return validGuid;
    }
}