using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Creator;
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
    Task<List<EventDescriptionDto>> GetAvailableEventsAsync(Guid agentId);
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
    private readonly IUserAppService _userAppService;
    private readonly IGAgentFactory _gAgentFactory;
    
    public SubscriptionAppService(
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        IUserAppService userAppService,
        IGAgentFactory gAgentFactory,
        ILogger<SubscriptionAppService> logger)
    {
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
        _userAppService = userAppService;
        _gAgentFactory = gAgentFactory;
    }
    
    public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync(Guid agentId)
    {
        var agent = _clusterClient.GetGrain<ICreatorGAgent>(agentId);
        var agentState = await agent.GetAgentAsync();
        _logger.LogInformation("GetAvailableEventsAsync id: {id} state: {state}", agentId, JsonConvert.SerializeObject(agentState));
        
        var eventDescriptionList = new List<EventDescriptionDto>();
        foreach (var evt in agentState.EventInfoList)
        {
            var eventType = evt.EventType;
            PropertyInfo[] properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var eventPropertyList = new List<EventProperty>();
            foreach (PropertyInfo property in properties)
            {
                var eventProperty = new EventProperty()
                {
                    Name = property.Name,
                    Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name,
                    Type = property.PropertyType.ToString()
                };
                eventPropertyList.Add(eventProperty);
            }
            
            eventDescriptionList.Add(new EventDescriptionDto()
            {
                EventType = eventType.FullName ?? eventType.Name,
                Description = evt.Description,
                EventProperties = eventPropertyList
            });
        }
        
        return eventDescriptionList;
    }
    
    

    public async Task<SubscriptionDto> SubscribeAsync(CreateSubscriptionDto createSubscriptionDto)
    {

      var  input = _objectMapper.Map<CreateSubscriptionDto, SubscribeEventInputDto>(createSubscriptionDto);
      var subscriptionStateAgent =
          _clusterClient.GetGrain<ISubscriptionGAgent>(GuidUtil.StringToGuid(createSubscriptionDto.AgentId.ToString()));
      
      input.UserId = _userAppService.GetCurrentUserId();
      var subscriptionState = await subscriptionStateAgent.SubscribeAsync(input);
      
      var agent = _clusterClient.GetGrain<ICreatorGAgent>(input.AgentId);
      var agentState = await agent.GetAgentAsync();
      var businessAgent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
      await businessAgent.RegisterAsync(subscriptionStateAgent);
      return _objectMapper.Map<EventSubscriptionState, SubscriptionDto>(subscriptionState);
    }

    public async Task CancelSubscriptionAsync(Guid subscriptionId)
    {
        var subscriptionStateAgent =
            _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId);
        var subscriptionState = await subscriptionStateAgent.GetStateAsync();
        var currentUserId = _userAppService.GetCurrentUserId();
        if (subscriptionState.UserId != currentUserId)
        {
            _logger.LogInformation("User {userId} is not allowed to cancel subscription {subscriptionId}.", currentUserId, subscriptionId);
            throw new UserFriendlyException("User is not allowed to cancel subscription");
        }
        
        var agent = _clusterClient.GetGrain<ICreatorGAgent>(subscriptionState.AgentId);
        await agent.UnregisterAsync(subscriptionStateAgent);
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
        var agent = _clusterClient.GetGrain<ICreatorGAgent>(dto.AgentId);
        var agentState = await agent.GetAgentAsync();
        _logger.LogInformation("PublishEventAsync id: {id} state: {state}", dto.AgentId, JsonConvert.SerializeObject(agentState));
        
        /*var currentUserId = _userAppService.GetCurrentUserId();
        if (agentState.UserId != currentUserId)
        {
            _logger.LogInformation("User {userId} is not allowed to publish event {eventType}.", currentUserId, dto.EventType);
            throw new UserFriendlyException("User is not allowed to publish event");
        }*/
        
        var eventList = agentState.EventInfoList;

        var eventDescription = eventList.Find(i => i.EventType.FullName == dto.EventType);
        if (eventDescription == null)
        {
            _logger.LogInformation("Type {type} could not be found.", dto.EventType);
            throw new UserFriendlyException("event could not be found");
        }
        
        var propertiesString = JsonConvert.SerializeObject(dto.EventProperties);
        var eventInstance = JsonConvert.DeserializeObject(propertiesString, eventDescription.EventType) as EventBase;

        if (eventInstance == null)
        {
            _logger.LogInformation("Event {type} could not be instantiated with param {param}", dto.EventType, propertiesString);
            throw new UserFriendlyException("event could not be instantiated");
        }
        
        await agent.PublishEventAsync(eventInstance);
        
    }
}