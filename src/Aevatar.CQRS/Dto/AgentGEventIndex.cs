using System;

namespace Aevatar.CQRS.Dto;

public class AgentGEventIndex : BaseIndex
{
    public Guid Id { get; set; }
    public Guid AgentPrimaryKey { get; set; }
    public string AgentGrainType { get; set; }
    public string EventName { get; set; }
    public DateTime Ctime { get; set; }
    public string EventJson { get; set; }
}