using System.Net.Http.Json;
using Aevatar.Application.Grains.Agents.Atomic;
using Aevatar.Application.Grains.Agents.Combination;
using Aevatar.AtomicAgent;
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
public class SubscriptionGAgent : GAgentBase<EventSubscriptionState, SubscriptionEvent>, ISubscriptionGAgent
{
    private readonly ILogger<SubscriptionGAgent> _logger;
    private readonly IClusterClient _clusterClient;
    public SubscriptionGAgent(ILogger<SubscriptionGAgent> logger, 
        IClusterClient clusterClient) : base(logger)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }
    
    public async Task<EventSubscriptionState> SubscribeAsync(SubscribeEventInputDto input)
    {
        RaiseEvent(new AddSubscriptionEvent()
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = input.AgentId,
            EventTypes = input.EventTypes.Count > 0 ? input.EventTypes : new List<string> { "ALL" },
            CallbackUrl = input.CallbackUrl,
            SubscriptionId = this.GetPrimaryKey()
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
        
        RaiseEvent(new CancelSubscriptionEvent()
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
        });
        await ConfirmEvents();
    }
    
    [AllEventHandler]
    public async Task HandleEventAsync(EventWrapperBase eventWrapperBase) 
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
                eventPushRequest.AtomicAgent = await GetAtomicAgentDtoFromEventGrainId(eventWrapper.PublisherGrainId);
                using var httpClient = new HttpClient();
                await httpClient.PostAsJsonAsync(State.CallbackUrl, eventPushRequest);
            }
        }
    }
    
    private async Task<AtomicAgentDto> GetAtomicAgentDtoFromEventGrainId(GrainId grainId)
    {
        Guid.TryParse(State.AgentId, out Guid combinationGuid);
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(combinationGuid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        foreach (var agentId in combinationData.AgentComponent)
        {
            var businessGuid = grainId.GetGuidKey().ToString();
            if (agentId.Value == businessGuid)
            {
                Guid.TryParse(agentId.Key, out Guid atomicGuid);
                var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(atomicGuid);
                var atomicAgentData = await atomicAgent.GetAgentAsync();
                var agentDto = new AtomicAgentDto()
                {
                    Id = agentId.Key,
                    Type = atomicAgentData.Type,
                    Name = atomicAgentData.Name
                };
                if (!atomicAgentData.Properties.IsNullOrEmpty())
                {
                    agentDto.Properties =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(atomicAgentData.Properties);
                }

                return agentDto;
            }
        }

        return new AtomicAgentDto();
    }

    public override async Task<string> GetDescriptionAsync()
    {
        return " a global event subscription and notification management agent";
    }
}

