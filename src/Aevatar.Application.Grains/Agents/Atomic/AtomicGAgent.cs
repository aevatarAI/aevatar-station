using System.ComponentModel;
using Aevatar.Agents.Atomic;
using Aevatar.Agents.Atomic.GEvents;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Atomic;

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
            Name = State.Name,
            Groups = State.Groups
        };
        return await Task.FromResult(agentData);
    }
    
    public async Task CreateAgentAsync(AtomicAgentData data)
    {
        _logger.LogInformation("CreateAgentAsync");
        RaiseEvent(new CreateAgentGEvent()
        {
            UserAddress = data.UserAddress,
            Id = Guid.NewGuid(),
            AtomicGAgentId = this.GetPrimaryKey(),
            Type = data.Type,
            Properties = data.Properties,
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
        _logger.LogInformation("DeleteAgentAsync");
        RaiseEvent(new DeleteAgentGEvent()
        {
        });
        await ConfirmEvents();
    }
    
    public async Task AddToGroupAsync(string groupId)
    {
        _logger.LogInformation("AddToGroupAsync");
        RaiseEvent(new AddToGroupGEvent()
        {
            GroupId = groupId
        });
        await ConfirmEvents();
    }
    
    public async Task RemoveFromGroupAsync(string groupId)
    {
        _logger.LogInformation("RemoveFromGroupAsync");
        RaiseEvent(new RemoveFromGroupGEvent()
        {
            GroupId = groupId
        });
        await ConfirmEvents();
    }
    
    protected override void GAgentTransitionState(AtomicGAgentState state, StateLogEventBase<AtomicAgentGEvent> @event)
    {
        switch (@event)
        {
            case CreateAgentGEvent createAgentGEvent:
                State.Id = createAgentGEvent.AtomicGAgentId;
                State.Properties = createAgentGEvent.Properties;
                State.UserAddress = createAgentGEvent.UserAddress;
                State.Type = createAgentGEvent.Type;
                State.Name = createAgentGEvent.Name;
                break;
            case UpdateAgentGEvent updateAgentGEvent:
                State.Properties = updateAgentGEvent.Properties;
                State.Name = updateAgentGEvent.Name;
                break;
            case DeleteAgentGEvent deleteAgentGEvent:
                State.UserAddress = "";
                State.Properties = "";
                State.Type = "";
                State.Name = "";
                State.Groups = new List<string>();
                break;
            case AddToGroupGEvent addToGroupGEvent:
                if (!State.Groups.Contains(addToGroupGEvent.GroupId))
                {
                    State.Groups.Add(addToGroupGEvent.GroupId);
                }
                break;
            case RemoveFromGroupGEvent removeFromGroupGEvent:
                if (State.Groups.Contains(removeFromGroupGEvent.GroupId))
                {
                    State.Groups.Remove(removeFromGroupGEvent.GroupId);
                }
                break;
        }
    }
}

public interface IAtomicGAgent : IStateGAgent<AtomicGAgentState>
{
    Task<AtomicAgentData> GetAgentAsync();
    Task CreateAgentAsync(AtomicAgentData data);
    Task UpdateAgentAsync(AtomicAgentData data);
    Task DeleteAgentAsync();
    Task AddToGroupAsync(string groupId);
    Task RemoveFromGroupAsync(string groupId);
}