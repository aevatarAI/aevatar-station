using System.Net.Http.Json;
using Aevatar.Agent;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Domain.Grains.Subscription;
using Aevatar.Subscription;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Subscription;
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class SubscriptionGAgent : GAgentBase<EventSubscriptionState, SubscriptionGEvent>, ISubscriptionGAgent
{
    private readonly ILogger<SubscriptionGAgent> _logger;
    private readonly IClusterClient _clusterClient;
    public SubscriptionGAgent(ILogger<SubscriptionGAgent> logger, 
        IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }
    
    public async Task<EventSubscriptionState> SubscribeAsync(SubscribeEventInputDto input)
    {
        RaiseEvent(new AddSubscriptionGEvent()
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = input.AgentId,
            EventTypes = input.EventTypes.Count > 0 ? input.EventTypes : new List<string> { "ALL" },
            CallbackUrl = input.CallbackUrl,
            SubscriptionId = this.GetPrimaryKey(),
            UserId = input.UserId
        });
        await ConfirmEvents();
        return State;
    }

    public async Task UnsubscribeAsync()
    {
        if (State.Status.IsNullOrEmpty())
        {
           return;
        }
        
        RaiseEvent(new CancelSubscriptionGEvent()
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
        });
        await ConfirmEvents();
    }
    
    [AllEventHandler]
    public async Task HandleSubscribedEventAsync(EventWrapperBase eventWrapperBase) 
    {
        if (eventWrapperBase is EventWrapper<EventBase> eventWrapper)
        {
            _logger.LogInformation("EventSubscriptionGAgent HandleRequestAllSubscriptionsEventAsync :" +
                                   JsonConvert.SerializeObject(eventWrapper));
            if (State.Status == "Active" && (State.EventTypes.IsNullOrEmpty() || State.EventTypes.Contains("ALL") || 
                                             State.EventTypes.Contains( eventWrapper.GetType().Name)))
            {
                var eventPushRequest = new EventPushRequest();
                eventPushRequest.AgentId = State.AgentId;
                eventPushRequest.EventId = eventWrapper.EventId;
                eventPushRequest.EventType = eventWrapper.Event.GetType().Name;
                eventPushRequest.Payload = JsonConvert.SerializeObject(eventWrapper.Event);
                eventPushRequest.AgentData = await GetAtomicAgentDtoFromEventGrainId(eventWrapper.PublisherGrainId);
                try
                {
                    using var httpClient = new HttpClient();
                    await httpClient.PostAsJsonAsync(State.CallbackUrl, eventPushRequest);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error sending event to callback url: {url} error: {err}", State.CallbackUrl, e.Message);
                }
            }
        }
    }
    
    private async Task<AgentDto> GetAtomicAgentDtoFromEventGrainId(GrainId grainId)
    {
        var guid = grainId.GetGuidKey();
        var agent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await agent.GetAgentAsync();
        
        return new AgentDto
        {
            Id = guid,
            AgentType = agentState.AgentType,
            Name = agentState.Name,
        };
    }

    public override async Task<string> GetDescriptionAsync()
    {
        return " a global event subscription and notification management agent";
    }
    
    protected override void GAgentTransitionState(EventSubscriptionState state, StateLogEventBase<SubscriptionGEvent> @event)
    {
        switch (@event)
        {
            case AddSubscriptionGEvent add:
                State.Id = add.SubscriptionId;
                State.AgentId = add.AgentId;
                State.EventTypes = add.EventTypes;
                State.CallbackUrl = add.CallbackUrl;
                State.Status = "Active";
                State.CreateTime = DateTime.Now;
                State.UserId = add.UserId;
                break;
            case CancelSubscriptionGEvent cancel:
                State.Status = "Cancelled";
                break;
        }
    }
}

