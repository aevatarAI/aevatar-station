using System;
using Aevatar.Agents.Atomic.GEvents;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Atomic;

public class AtomicGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UserAddress { get; set; }
    [Id(2)] public string GroupId { get; set; }
    [Id(3)] public string Type { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string BusinessAgentId { get; set; }
    [Id(6)] public string Properties { get; set; }
    
    public void Apply(CreateAgentGEvent createAgentGEvent)
    {
        Id = createAgentGEvent.AtomicGAgentId;
        Properties = createAgentGEvent.Properties;
        UserAddress = createAgentGEvent.UserAddress;
        Type = createAgentGEvent.Type;
        Name = createAgentGEvent.Name;
        BusinessAgentId = createAgentGEvent.BusinessAgentId;
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
        BusinessAgentId = "";
    }
    
    public void Apply(RegisterToGroupGEvent registerToGroupGEvent)
    {
        GroupId = registerToGroupGEvent.GroupId;
    }
}