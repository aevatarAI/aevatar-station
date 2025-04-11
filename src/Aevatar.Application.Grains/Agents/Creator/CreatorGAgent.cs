using System.ComponentModel;
using System.Reflection;
using Aevatar.Agent;
using Aevatar.Agents.Creator;
using Aevatar.Agents.Creator.GEvents;
using Aevatar.Agents.Creator.Models;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.Application.Grains.Agents.Creator;

public class CreatorGAgent : GAgentBase<CreatorGAgentState, CreatorAgentGEvent>, ICreatorGAgent
{
    private readonly ILogger<CreatorGAgent> _logger;

    public CreatorGAgent(ILogger<CreatorGAgent> logger) 
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for creating and grouping other agents");
    }
    
    public async Task<CreatorGAgentState> GetAgentAsync()
    {
        _logger.LogInformation("GetAgentAsync {state}", JsonConvert.SerializeObject(State));
        return State;
    }
    
    public async Task CreateAgentAsync(AgentData agentData)
    {
        _logger.LogInformation("CreateAgentAsync");
        RaiseEvent(new CreateAgentGEvent()
        {
            UserId = agentData.UserId,
            Id = Guid.NewGuid(),
            AgentId = this.GetPrimaryKey(),
            AgentType = agentData.AgentType,
            Properties = agentData.Properties,
            Name = agentData.Name,
            BusinessAgentGrainId = agentData.BusinessAgentGrainId
        });
        await ConfirmEvents();
    }
    
    public async Task UpdateAgentAsync(UpdateAgentInput dto)
    {
        _logger.LogInformation("UpdateAgentAsync");
        RaiseEvent(new UpdateAgentGEvent()
        {
            Id = Guid.NewGuid(),
            Properties = dto.Properties,
            Name = dto.Name
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
    
    public async Task UpdateAvailableEventsAsync(List<Type>? eventTypeList)
    {   
        _logger.LogInformation("UpdateAvailableEventsAsync {list}", JsonConvert.SerializeObject(eventTypeList));
        if (eventTypeList == null)
        {
            _logger.LogInformation("UpdateAvailableEventsAsync No eventTypeList");
            return;
        }

        var eventDescriptionList = new List<EventDescription>();
        foreach (var t in eventTypeList)
        {
            eventDescriptionList.Add(new EventDescription()
            {
                EventType = t,
                Description = t.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description",
            });
        }
        
        RaiseEvent(new UpdateAvailableEventsGEvent()
        {
            EventInfoList = eventDescriptionList
        });
        await ConfirmEvents();
        _logger.LogInformation("UpdateAvailableEventsAsync Finish {list}", JsonConvert.SerializeObject(eventDescriptionList));
    }
    
    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        Logger.LogInformation( "publish event: {event}", @event);
        await PublishAsync(@event);
    }
    
    protected override void GAgentTransitionState(CreatorGAgentState state, StateLogEventBase<CreatorAgentGEvent> @event)
    {
        switch (@event)
        {
            case CreateAgentGEvent createAgentGEvent:
                State.Id = createAgentGEvent.AgentId;
                State.Properties = createAgentGEvent.Properties;
                State.UserId = createAgentGEvent.UserId;
                State.AgentType = createAgentGEvent.AgentType;
                State.Name = createAgentGEvent.Name;
                State.BusinessAgentGrainId = createAgentGEvent.BusinessAgentGrainId;
                State.CreateTime = DateTime.Now;
                break;
            case UpdateAgentGEvent updateAgentGEvent:
                State.Properties = updateAgentGEvent.Properties;
                State.Name = updateAgentGEvent.Name;
                break;
            case DeleteAgentGEvent deleteAgentGEvent:
                State.UserId = Guid.Empty;
                State.AgentType = "";
                State.Name = "";
                State.Properties = null;
                State.BusinessAgentGrainId = default;
                break;
            case UpdateAvailableEventsGEvent updateSubscribedEventInfoGEvent:
                State.EventInfoList = updateSubscribedEventInfoGEvent.EventInfoList;
                break;
        }
    }
}

public interface ICreatorGAgent : IStateGAgent<CreatorGAgentState>
{
    Task<CreatorGAgentState> GetAgentAsync();
    Task CreateAgentAsync(AgentData agentData);
    Task UpdateAgentAsync(UpdateAgentInput dto);
    Task DeleteAgentAsync();
    Task PublishEventAsync<T>(T @event) where T : EventBase;
    Task UpdateAvailableEventsAsync(List<Type>? eventTypeList);
}