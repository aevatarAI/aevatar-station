using System;
using System.Collections.Generic;

namespace Aevatar.CQRS;

public class AgentEventLogsDto
{
    public List<AgentEventDto> Items { get; set; }
    public long TotalCount { get; set; }
}

public class AgentEventDto
{
    public Guid Id { get; set; }
    public Guid AgentPrimaryKey { get; set; }
    public string AgentGrainType { get; set; }
    public string EventName { get; set; }
    public DateTime Ctime { get; set; }
    public string EventJson{ get; set; }
}