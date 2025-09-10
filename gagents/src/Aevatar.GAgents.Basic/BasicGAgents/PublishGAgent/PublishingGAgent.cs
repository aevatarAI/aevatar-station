using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Basic.PublishGAgent;

[GenerateSerializer]
public class PublishingAgentState : StateBase
{
}

[GAgent(nameof(PublishingGAgent))]
public class PublishingGAgent : GAgentBase<PublishingAgentState, PublishingStateLogEvent>, IPublishingGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent to be used for publishing new events.");
    }

    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        Logger.LogInformation($"PublishingAgent publish {@event}");
        await PublishAsync(@event);
    }
}