using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;

[GenerateSerializer]
public class StartWorkflowCoordinatorEvent : EventBase
{
    [Id(0)] public string? InitContent { get; set; } = null;
}