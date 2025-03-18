using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.Core;

public abstract partial class GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
{
    private Guid? _correlationId;

    protected async Task PublishAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase
    {
        await SendEventUpwardsAsync(eventWrapper);
        await SendEventDownwardsAsync(eventWrapper);
    }

    protected async Task<Guid> PublishAsync<T>(T @event) where T : EventBase
    {
        _correlationId ??= Guid.NewGuid();
        @event.CorrelationId = _correlationId;
        @event.PublisherGrainId = this.GetGrainId();
        Logger.LogInformation("Published event {@Event}, {CorrelationId}", @event, _correlationId);

        var eventId = Guid.NewGuid();
        if (State.Parent == null)
        {
            Logger.LogInformation(
                "Event is the first time appeared to silo: {@Event}", @event);
            // This event is the first time appeared to silo.
            await SendEventToSelfAsync(new EventWrapper<T>(@event, eventId, this.GetGrainId()));
        }
        else
        {
            Logger.LogInformation(
                "{GrainId} is publishing event upwards: {EventJson}",
                this.GetGrainId().ToString(), JsonConvert.SerializeObject(@event));
            await PublishEventUpwardsAsync(@event, eventId);
        }

        return eventId;
    }

    private async Task PublishEventUpwardsAsync<T>(T @event, Guid eventId) where T : EventBase
    {
        await SendEventUpwardsAsync(new EventWrapper<T>(@event, eventId, this.GetGrainId()));
    }

    private async Task SendEventUpwardsAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase
    {
        if (State.Parent == null)
        {
            return;
        }
        var stream = GetEventBaseStream(State.Parent.Value);
        await stream.OnNextAsync(eventWrapper);
    }

    private async Task SendEventToSelfAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase
    {
        Logger.LogInformation(
            $"{this.GetGrainId().ToString()} is sending event to self: {JsonConvert.SerializeObject(eventWrapper)}");
        var streamOfThisGAgent = GetEventBaseStream(this.GetGrainId());
        await streamOfThisGAgent.OnNextAsync(eventWrapper);
    }

    private async Task SendEventDownwardsAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase
    {
        if (State.Children.IsNullOrEmpty())
        {
            return;
        }

        Logger.LogInformation($"{this.GetGrainId().ToString()} has {State.Children.Count} children.");

        foreach (var stream in State.Children.Select(GetEventBaseStream))
        {
            await stream.OnNextAsync(eventWrapper);
        }
    }
}