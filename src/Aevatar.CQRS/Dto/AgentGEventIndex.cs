
using System;
using Nest;

namespace Aevatar.CQRS.Dto;

public class AgentGEventIndex : BaseIndex
{
    [Keyword]public Guid Id { get; set; }
    [Keyword]public Guid GrainId { get; set; }
    [Keyword]public string GrainType { get; set; }
    public DateTime Ctime { get; set; }
    public string EventJson{ get; set; }
}