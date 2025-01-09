using System;
using System.Collections.Generic;
using Aevatar.Agents.Atomic.GEvents;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Atomic;

public class AtomicGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UserAddress { get; set; }
    [Id(2)] public List<string> Groups { get; set; } = new List<string>();
    [Id(3)] public string Type { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string Properties { get; set; }
    
    public void Apply(CreateAgentGEvent createAgentGEvent)
    {
        Id = createAgentGEvent.Id;
        Properties = createAgentGEvent.Properties;
        UserAddress = createAgentGEvent.UserAddress;
        Type = createAgentGEvent.Type;
        Name = createAgentGEvent.Name;
    }
    
    public void Apply(UpdateAgentGEvent updateAgentGEvent)
    {
        Properties = updateAgentGEvent.Properties;
        Name = updateAgentGEvent.Name;
    }
    
    public void Apply(DeleteAgentGEvent deleteAgentGEvent)
    {
        UserAddress = "";
        Properties = "";
        Type = "";
        Name = "";
        Groups = new List<string>();
    }
    
    public void Apply(AddToGroupGEvent addToGroupGEvent)
    {
        if (!Groups.Contains(addToGroupGEvent.GroupId))
        {
            Groups.Add(addToGroupGEvent.GroupId);
        }
    }
    
    public void Apply(RemoveFromGroupGEvent removeFromGroupGEvent)
    {
        if (Groups.Contains(removeFromGroupGEvent.GroupId))
        {
            Groups.Remove(removeFromGroupGEvent.GroupId);
        }
    }
}