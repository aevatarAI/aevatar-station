using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;

namespace Aevatar.GAgents.GroupChat.Feature.Extension;

public static class IGAgentExtension
{
    public static async Task<bool> AddGroupChat(this IGAgent agent, IClusterClient clusterClient, string topic)
    {
        var blackboard = clusterClient.GetGrain<IBlackboardGAgent>(Guid.NewGuid());
        if (await blackboard.SetTopic(topic) == false)
        {
            return false;
        }

        await agent.RegisterAsync(blackboard);
        var coordinatorGAgent = clusterClient.GetGrain<ICoordinatorGAgent>(blackboard.GetPrimaryKey());
        await agent.RegisterAsync(coordinatorGAgent);

        await coordinatorGAgent.StartAsync(blackboard.GetPrimaryKey());

        return true;
    }

    public static async Task AddWorkflowGroupChat(this IGAgent agent, IGAgentFactory agentFactory, List<WorkflowUnitDto> workflowUnitList)
    {
        var workflowCoordinator = await agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = workflowUnitList
        });
        
        await agent.RegisterAsync(workflowCoordinator);
    }
}