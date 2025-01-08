using System.ComponentModel;
using Aevatar.AtomicAgent.Agent.GEvents;
using Aevatar.AtomicAgent.Models;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.AtomicAgent.Agent;

[Description("Handle atomic agent")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AtomicGAgent : GAgentBase<AtomicGAgentState, AtomicAgentGEvent>, IAtomicGAgent
{
    private readonly ILogger<AtomicGAgent> _logger;

    public AtomicGAgent(ILogger<AtomicGAgent> logger) : base(logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an atomic agent responsible for creating other agents such as TwitterGAgent and TelegramGAgent");
    }
    
    public async Task<AtomicAgentData?> GetAgentAsync()
    {
        _logger.LogInformation("GetAgentAsync");
        var agentData = new AtomicAgentData()
        {
            Id = this.GetPrimaryKey(),
            UserAddress = State.UserAddress,
            Type = State.Type,
            Properties = State.Properties,
            BusinessAgentId = State.BusinessAgentId,
            Name = State.Name,
            GroupId = State.GroupId
        };
        return await Task.FromResult(agentData);
    }
    
    public async Task CreateAgentAsync(AtomicAgentData data)
    {
        _logger.LogInformation("CreateAgentAsync");
        RaiseEvent(new CreateAgentGEvent()
        {
            UserAddress = data.UserAddress,
            Id = this.GetPrimaryKey(),
            Type = data.Type,
            Properties = data.Properties,
            BusinessAgentId = data.BusinessAgentId,
            Name = data.Name
        });
        await ConfirmEvents();
    }
    
    public async Task UpdateAgentAsync(AtomicAgentData data)
    {
        _logger.LogInformation("UpdateAgentAsync");
        RaiseEvent(new UpdateAgentGEvent()
        {
            Properties = data.Properties,
            Name = data.Name
        });
        await ConfirmEvents();
    }
    
    public async Task DeleteAgentAsync()
    {
        _logger.LogInformation("UpdateAgentAsync");
        RaiseEvent(new DeleteAgentGEvent()
        {
        });
        await ConfirmEvents();
    }
    
    public async Task RegisterToGroupAsync(string groupId)
    {
        _logger.LogInformation("SetUseFlagAsync");
        RaiseEvent(new RegisterToGroupGEvent()
        {
            GroupId = groupId
        });
        await ConfirmEvents();
    }
}

public interface IAtomicGAgent : IStateGAgent<AtomicGAgentState>
{
    Task<AtomicAgentData?> GetAgentAsync();
    Task CreateAgentAsync(AtomicAgentData data);
    Task UpdateAgentAsync(AtomicAgentData data);
    Task DeleteAgentAsync();
    Task RegisterToGroupAsync(string groupId);
}