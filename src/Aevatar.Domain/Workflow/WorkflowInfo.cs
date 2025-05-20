using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Workflow;

public class WorkflowInfo: FullAuditedAggregateRoot<Guid>
{
    public string WorkflowGrainId { get; set; }
    public List<WorkflowUintInfo> WorkUnitList { get; set; } = new List<WorkflowUintInfo>();
}