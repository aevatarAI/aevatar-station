using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agents.Combination;
using Aevatar.Application.Grains.Agents.Combination;
using Aevatar.Application.Grains.Agents.Investment;
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
    Task<List<EventDescriptionDto>> GetAvailableEventsAsync(string agentId);
    Task<SubscriptionDto> SubscribeAsync(CreateSubscriptionDto createSubscriptionDto);
    Task CancelSubscriptionAsync(Guid subscriptionId);
    Task<SubscriptionDto> GetSubscriptionAsync(Guid subscriptionId);
    Task PublishEventAsync(PublishEventDto dto);
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
    
    public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync(string agentId)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(ParseGuid(agentId));
        var combinationData = await combinationAgent.GetCombinationAsync();
        var dto = _objectMapper.Map<List<EventDescription>, List<EventDescriptionDto>>(combinationData.EventInfoList);
        return dto;
    }
    

    public async Task<SubscriptionDto> SubscribeAsync(CreateSubscriptionDto createSubscriptionDto)
    {

      var  input = _objectMapper.Map<CreateSubscriptionDto, SubscribeEventInputDto>(createSubscriptionDto);
      var subscriptionStateAgent =
          _clusterClient.GetGrain<ISubscriptionGAgent>(GuidUtil.StringToGuid(createSubscriptionDto.AgentId));
      
      var eventData = await subscriptionStateAgent.GetAllSubscribedEventsAsync();
      
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

    public async Task PublishEventAsync(PublishEventDto dto)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(ParseGuid(dto.AgentId));
        var combinationData = await combinationAgent.GetCombinationAsync();
        var eventList = combinationData.EventInfoList;

        var eventDescription = eventList.Find(i => i.EventType.Name == dto.EventType);
        if (eventDescription == null)
        {
            _logger.LogInformation("Type {type} could not be found.", dto.EventType);
            throw new UserFriendlyException("event could not be found");
        }
        
        var eventType = eventDescription.EventType;
        object? eventInstance = Activator.CreateInstance(eventType);
        if (eventInstance == null)
        {
            _logger.LogInformation("Type {type} could not be instantiated.", dto.EventType);
            throw new UserFriendlyException("event could not be instantiated");
        }
        
        foreach (var property in dto.EventProperties)
        {
            string propertyName = property.Key;
            object propertyValue = property.Value;
            
            PropertyInfo? propInfo = eventType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null || !propInfo.CanWrite)
            {
                _logger.LogInformation("Property {propertyName} not found or cannot be written.", propertyName);
                throw new UserFriendlyException("property could not be found or cannot be written");
            }

            try
            {
                object? convertedValue = Convert.ChangeType(propertyValue, propInfo.PropertyType);
                propInfo.SetValue(eventInstance, convertedValue);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Failed to convert property value: {propertyName} - {propertyValue} - {ex}", propertyName, propertyValue, ex);
                throw new UserFriendlyException("property could not be converted");
            }
        }
        
        await combinationAgent.PublishEventAsync((EventBase)eventInstance);
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