using System.Net.Http.Json;
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
    public SubscriptionGAgent(ILogger<SubscriptionGAgent> logger) : base(logger)
    {
        _logger = logger;
    }
    
    public async Task<EventSubscriptionState> SubscribeAsync(SubscribeEventInputDto input)
    {
        //todo  group register
        RaiseEvent(new AddSubscriptionEvent()
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = input.AgentId,
            EventTypes = input.EventTypes.Count > 0 ? input.EventTypes : new List<string> { "ALL" },
            CallbackUrl = input.CallbackUrl,
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
        //todo  group unregister
        RaiseEvent(new CancelSubscriptionEvent()
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
        });
        await ConfirmEvents();
    }
    
    [AllEventHandler]
    public async Task HandleRequestAllSubscriptionsEventAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase
    {
        _logger.LogInformation("EventSubscriptionGAgent HandleRequestAllSubscriptionsEventAsync :" +
                               JsonConvert.SerializeObject(eventWrapper));
        if (State.Status.IsNullOrEmpty() && State.Status == "active")
        {
            if (State.EventTypes.Contains( eventWrapper.GetType().Name))
            {
                var eventPushRequest = new EventPushRequest();
                eventPushRequest.AgentId = State.AgentId;
                eventPushRequest.EventId = eventWrapper.EventId;
                eventPushRequest.EventType = eventWrapper.Event.GetType().Name;
                eventPushRequest.Payload = JsonConvert.SerializeObject(eventWrapper.Event);
                eventPushRequest.AtomicAgent = new AtomicAgentDto()
                {
                    //todo query agent
                };
                using var httpClient = new HttpClient();
                await httpClient.PostAsJsonAsync(State.CallbackUrl, eventPushRequest);
            }
        }
    }

    public override async Task<string> GetDescriptionAsync()
    {
        return " a global event subscription and notification management agent";
    }
}

