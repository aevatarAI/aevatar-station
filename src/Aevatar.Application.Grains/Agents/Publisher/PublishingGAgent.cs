using Aevatar.Agents.Publisher;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Sender;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Publisher;

[GenerateSerializer]
public class PublishingAgentState : StateBase
{
}

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent("publish")]
public class PublishingGAgent : GAgentBase<PublishingAgentState, PublishingGEvent>, IPublishingGAgent
{
    public PublishingGAgent() 
    {
    }

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