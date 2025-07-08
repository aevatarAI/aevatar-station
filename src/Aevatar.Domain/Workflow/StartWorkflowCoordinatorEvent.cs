using System;
using Aevatar.Core.Abstractions;

namespace Aevatar.Workflow;

public class StartWorkflowCoordinatorEvent : EventBase
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; }
} 