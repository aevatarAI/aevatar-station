using System.ComponentModel;
using Aevatar.Application.Grains.Agents.MarketLeader;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Developer;

[GAgent("Developer")]
public class DeveloperGAgent : GAgentBase<DeveloperAgentState, DeveloperGEvent>, IDeveloperGAgent
{
    public DeveloperGAgent(ILogger<MarketLeaderGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a social event is published.");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(FinishEvent eventData)
    {
        Logger.LogInformation("receive");
    }
}

public interface IDeveloperGAgent :  IStateGAgent<DeveloperAgentState>
{

}