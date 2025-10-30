using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Workflow.Core.Events;

[GenerateSerializer]
public class StartWorkflowCoordinatorEvent : EventBase
{
    [Id(0)] public string? InitContent { get; set; } = null;
}