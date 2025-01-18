using System.ComponentModel;
using System.Reflection;
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
            Id = Guid.NewGuid(),
            CombineGAgentId = this.GetPrimaryKey(),
            UserAddress = data.UserAddress,
            Name = data.Name,
            GroupId = data.GroupId,
            AgentComponent = data.AgentComponent
        });
        await ConfirmEvents();
    }

    public async Task<CombinationAgentData> GetCombinationAsync()
    {
        _logger.LogInformation("GetCombinationAsync");
        var combinationData = new CombinationAgentData()
        {
            Id = this.GetPrimaryKey(),
            Name = State.Name,
            GroupId = State.GroupId,
            AgentComponent = State.AgentComponent,
            UserAddress = State.UserAddress,
            Status = State.Status,
            EventInfoList = State.EventInfoList
        };
        return combinationData;
    }

    public async Task UpdateCombinationAsync(CombinationAgentData data)
    {
        _logger.LogInformation("UpdateAgentAsync");
        RaiseEvent(new UpdateCombinationGEvent()
        {
            Name = data.Name,
            AgentComponent = data.AgentComponent
        });
        await ConfirmEvents();
    }
    
    public Task<AgentStatus> GetStatusAsync()
    {
        return Task.FromResult(State.Status);
    }

    public async Task DeleteCombinationAsync()
    {
        _logger.LogInformation("DeleteAgentAsync");
        RaiseEvent(new DeleteCombinationGEvent()
        {
        });
        await ConfirmEvents();
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
    
    protected override Task OnRegisterAgentAsync(Guid agentGuid)
    {
        ++State.RegisteredAgents;
        return Task.CompletedTask;
    }

    protected override Task OnUnregisterAgentAsync(Guid agentGuid)
    {
        --State.RegisteredAgents;
        return Task.CompletedTask;
    }

    public async Task UpdateSubscribedEventAsync(List<Type>? eventTypeList)
    {
        if (eventTypeList == null)
        {
            return;
        }

        var originEventList = State.EventInfoList;
        var eventInfoList = new List<EventDescription>();
        foreach (var t in eventTypeList)
        {
            if (originEventList.Exists(x => x.EventType.Name == t.Name) || eventInfoList.Exists(x => x.EventType.Name == t.Name))
            {
                continue;
            }
            
            PropertyInfo[] properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var eventPropertyList = new List<EventProperty>();
            foreach (PropertyInfo property in properties)
            {
                var eventProperty = new EventProperty()
                {
                    Name = property.Name,
                    Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name,
                    Type = property.PropertyType.ToString()
                };
                eventPropertyList.Add(eventProperty);
            }
            
            eventInfoList.Add(new EventDescription()
            {
                EventType = t,
                Description = t.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description available",
                EventProperties = eventPropertyList
            });
        }
        
        originEventList.AddRange(eventInfoList);
        RaiseEvent(new UpdateSubscribedEventInfoGEvent()
        {
            EventInfoList = originEventList
        });
        await ConfirmEvents();
    }
    
    // [EventHandler]
    // public async Task HandleSubscribedEventAsync(SubscribedEventListEvent eventData)
    // {
    //     var allEvents = eventData.Value.Values.SelectMany(list => list).ToList();
    //     var eventInfoList = new List<EventInfo>();
    //     foreach (var t in allEvents)
    //     {
    //         PropertyInfo[] properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
    //         var eventPropertyList = new List<EventProperty>();
    //         foreach (PropertyInfo property in properties)
    //         {
    //             var eventProperty = new EventProperty()
    //             {
    //                 Name = property.Name,
    //                 Type = property.PropertyType.ToString()
    //             };
    //             eventPropertyList.Add(eventProperty);
    //         }
    //         
    //         eventInfoList.Add(new EventInfo()
    //         {
    //             EventType = t.Name,
    //             EventProperties = eventPropertyList
    //         });
    //     }
    //     
    //     _logger.LogInformation("HandleSubscribedEventAsync");
    //     RaiseEvent(new UpdateSubscribedEventInfoGEvent()
    //     {
    //         EventInfoList = eventInfoList
    //     });
    //     await ConfirmEvents();
    //     
    // }
    
    protected override void GAgentTransitionState(CombinationGAgentState state, StateLogEventBase<CombinationAgentGEvent> @event)
    {
        switch (@event)
        {
            case CombineAgentGEvent combineAgentGEvent:
                State.Id = combineAgentGEvent.CombineGAgentId;
                State.Name = combineAgentGEvent.Name;
                State.GroupId = combineAgentGEvent.GroupId;
                State.UserAddress = combineAgentGEvent.UserAddress;
                State.Status = AgentStatus.Running;
                State.AgentComponent = combineAgentGEvent.AgentComponent;
                break;
            case UpdateCombinationGEvent combineCombinationGEvent:
                State.Name = combineCombinationGEvent.Name;
                State.AgentComponent = combineCombinationGEvent.AgentComponent;
                break;
            case DeleteCombinationGEvent deleteCombinationGEvent:
                State.Name = "";
                State.AgentComponent = new ();
                State.GroupId = "";
                State.Status = AgentStatus.Deleted;
                State.UserAddress = "";
                break;
            case UpdateSubscribedEventInfoGEvent updateSubscribedEventInfoGEvent:
                State.EventInfoList = updateSubscribedEventInfoGEvent.EventInfoList;
                break;
        }
    }
}


public interface ICombinationGAgent : IStateGAgent<CombinationGAgentState>
{
    Task CombineAgentAsync(CombinationAgentData data);
    Task<CombinationAgentData> GetCombinationAsync();
    Task UpdateCombinationAsync(CombinationAgentData data);
    Task<AgentStatus> GetStatusAsync();
    Task DeleteCombinationAsync();
    Task PublishEventAsync<T>(T @event) where T : EventBase;
    Task UpdateSubscribedEventAsync(List<Type>? eventTypeList);
}