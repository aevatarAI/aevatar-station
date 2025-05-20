using System.Collections.Generic;
using Orleans.Runtime;

namespace Aevatar.Agents;

public class WorkflowAgentsDto
{
    public List<WorkflowAgentDefinesDto> WorkUnitRelations { get; set; }
}

public class WorkflowAgentDefinesDto
{
    public string GrainId { get; set; }
    public string? NextGrainId { get; set; }
    public float XPosition { get; set; }
    public float YPosition { get; set; }
}