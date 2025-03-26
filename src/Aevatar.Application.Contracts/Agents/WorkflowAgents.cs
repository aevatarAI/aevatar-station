using System.Collections.Generic;
using Orleans.Runtime;

namespace Aevatar.Agents;

public class WorkflowAgentsDto
{
    public List<WorkflowAgentDefinesDto> AgentList { get; set; }
}

public class WorkflowAgentDefinesDto
{
    public GrainId AgentGrainId { get; set; }
    public GrainId ParentAgentGrainId { get; set; }
}