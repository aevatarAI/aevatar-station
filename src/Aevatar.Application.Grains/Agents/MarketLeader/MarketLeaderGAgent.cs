using System.ComponentModel;
using Aevatar.Application.Grains.Agents.Investment;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.MarketLeader;
[Description("Marketing department，I can handle tasks related to the marketing department.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent("MyAgent", "MyNs")]
public class MarketLeaderGAgent : GAgentBase<MarketLeaderAgentState, MarketLeaderGEvent>, IMarketLeaderGAgent
{
    public MarketLeaderGAgent(ILogger<MarketLeaderGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a social event is published.");
    }

    [EventHandler]
    public async Task HandleEventAsync(SendMessageEvent eventData)
    {
        // Logger.LogInformation($"{this.GetType().ToString()} ExecuteAsync: Market Leader analyses content:{eventData.Message}");
        await PublishAsync(new FinishEvent()
        {
            Message = eventData.Message
        });
    }
}

public interface IMarketLeaderGAgent :  IStateGAgent<MarketLeaderAgentState>
{

}