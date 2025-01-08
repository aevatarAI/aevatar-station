using System.ComponentModel;
using Aevatar.Agents.Combination;
using Aevatar.Agents.Combination.GEvents;
using Aevatar.Agents.Combination.Models;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Combination;

[Description("Handle Agent Combination")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class CombinationGAgent : GAgentBase<CombinationGAgentState, CombinationAgentGEvent>, ICombinationGAgent
{
    private readonly ILogger<CombinationGAgent> _logger;

    public CombinationGAgent(ILogger<CombinationGAgent> logger) : base(logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible combine atomic agents into a group");
    }
    
    public async Task CombineAgentAsync(CombinationAgentData data)
    {
        _logger.LogInformation("CombineAgentAsync");
        RaiseEvent(new CombineAgentGEvent()
        {
            Id = this.GetPrimaryKey(),
            UserAddress = data.UserAddress,
            Name = data.Name,
            GroupId = data.GroupId,
            AgentComponent = data.AgentComponent
        });
        await ConfirmEvents();
    }
    
    public Task<bool> AgentExistAsync()
    {
        var exist = State.Status == AgentStatus.Running || State.Status == AgentStatus.Stopped;
        return Task.FromResult(exist);
    }

    public Task<AgentStatus> GetStatusAsync()
    {
        return Task.FromResult(State.Status);
    }
}


public interface ICombinationGAgent : IStateGAgent<CombinationGAgentState>
{
    Task CombineAgentAsync(CombinationAgentData data);
    // Task UpdateAgentAsync(AgentData data);
    // Task DeleteAgentAsync();
    Task<AgentStatus> GetStatusAsync();
}