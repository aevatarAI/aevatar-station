using System.Collections.Generic;

namespace Aevatar.Agents;

public class WorkflowWithGrainIdRequestDto
{
    public string WorkflowGrainId { get; set; }
    public List<WorkflowAgentDefinesDto> WorkUnitRelations { get; set; }
}