using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using Newtonsoft.Json;
using Shouldly;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

public sealed class WorkFlowViewTest : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public WorkFlowViewTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task WorkflowViewTest()
    {
        var workflowViewAgent = await _agentFactory.GetGAgentAsync<IWorkflowViewGAgent>(Guid.NewGuid());
        var workflowViewConfig = new WorkflowViewConfigDto();
        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();
        workflowViewConfig.Name = "workflowView";
        workflowViewConfig.WorkflowNodeList.Add(new WorkflowNodeDto()
        {
            NodeId = nodeId1,
            AgentType = fread.GetGrainId().Type.ToString(),
            JsonProperties =  JsonConvert.SerializeObject( new Dictionary<string, object>()
            {
                {"MemberName","fread"}
            }),
            Name = "fread"
        });
        workflowViewConfig.WorkflowNodeList.Add(new WorkflowNodeDto()
        {
            NodeId = nodeId2,
            AgentType = moni.GetGrainId().Type.ToString(),
            JsonProperties =  JsonConvert.SerializeObject( new Dictionary<string, object>()
            {
                {"MemberName","moni"}
            }),
            Name = "moni"
        });
        workflowViewConfig.WorkflowNodeUnitList.Add(new WorkflowNodeUnitDto()
        {
            NodeId = nodeId1,
            NextNodeId = nodeId2
        });
        await workflowViewAgent.ConfigAsync(workflowViewConfig);
        await Task.Delay(2000);
        var viewState = await workflowViewAgent.GetStateAsync();
        viewState.Name.ShouldBe(workflowViewConfig.Name);
        viewState.WorkflowNodeList.Count.ShouldBe(2);
        viewState.WorkflowNodeList[0].NodeId.ShouldBe(nodeId1);
        viewState.WorkflowNodeList[0].AgentType.ShouldBe(fread.GetGrainId().Type.ToString());
        viewState.WorkflowNodeList[0].JsonProperties.ShouldBe(JsonConvert.SerializeObject( new Dictionary<string, object>()
        {
            {"MemberName","fread"}
        }));
        viewState.WorkflowNodeList[1].NodeId.ShouldBe(nodeId2);
        viewState.WorkflowNodeList[1].AgentType.ShouldBe(moni.GetGrainId().Type.ToString());
        viewState.WorkflowNodeList[1].JsonProperties.ShouldBe(JsonConvert.SerializeObject( new Dictionary<string, object>()
        {
            {"MemberName","moni"}
        }));
        
        viewState.WorkflowNodeUnitList.Count.ShouldBe(1);
        viewState.WorkflowNodeUnitList[0].NodeId.ShouldBe(nodeId1);
        viewState.WorkflowNodeUnitList[0].NextNodeId.ShouldBe(nodeId2);

        var nodeId3 = Guid.NewGuid();
        workflowViewConfig.WorkflowNodeList.Add(new WorkflowNodeDto()
        {
            NodeId = nodeId3,
            AgentType = fread.GetGrainId().Type.ToString(),
            JsonProperties =  JsonConvert.SerializeObject( new Dictionary<string, object>()
            {
                {"MemberName","fread"}
            }),
            Name = "fread1"
        });
        await workflowViewAgent.ConfigAsync(workflowViewConfig);
        await Task.Delay(2000);
        viewState = await workflowViewAgent.GetStateAsync();
        viewState.WorkflowNodeList.Count.ShouldBe(3);
        viewState.WorkflowNodeList[2].NodeId.ShouldBe(nodeId3);
    }

    [Fact]
    public async Task WorkflowViewTest_Fail()
    {
        var workflowViewAgent = await _agentFactory.GetGAgentAsync<IWorkflowViewGAgent>(Guid.NewGuid());
        var workflowViewConfig = new WorkflowViewConfigDto();
        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();
        workflowViewConfig.Name = "workflowView";
        workflowViewConfig.WorkflowNodeList.Add(new WorkflowNodeDto()
        {
            NodeId = nodeId1,
            AgentType = fread.GetGrainId().Type.ToString(),
            JsonProperties =  JsonConvert.SerializeObject( new Dictionary<string, object>()
            {
                {"MemberName","fread"}
            }),
            Name = "fread",
            AgentId = fread.GetPrimaryKey()
        });
        workflowViewConfig.WorkflowNodeList.Add(new WorkflowNodeDto()
        {
            NodeId = nodeId2,
            AgentType = moni.GetGrainId().Type.ToString(),
            JsonProperties =  JsonConvert.SerializeObject( new Dictionary<string, object>()
            {
                {"MemberName","moni"}
            }),
            Name = "moni",
            AgentId = moni.GetPrimaryKey()
        });
        workflowViewConfig.WorkflowNodeUnitList.Add(new WorkflowNodeUnitDto()
        {
            NodeId = nodeId1,
            NextNodeId = nodeId2
        });
        workflowViewConfig.WorkflowCoordinatorGAgentId = Guid.NewGuid();
        var oldWorkflowCoordinatorGAgentId = workflowViewConfig.WorkflowCoordinatorGAgentId;
        await workflowViewAgent.ConfigAsync(workflowViewConfig);
        
        workflowViewConfig.WorkflowCoordinatorGAgentId = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await workflowViewAgent.ConfigAsync(workflowViewConfig));
        exception.Message.ShouldContain("WorkflowCoordinatorGAgentId not support change");

        workflowViewConfig.WorkflowCoordinatorGAgentId = oldWorkflowCoordinatorGAgentId;
        workflowViewConfig.WorkflowNodeList[0].AgentId = Guid.NewGuid();
        exception = await Assert.ThrowsAsync<ArgumentException>(async () => await workflowViewAgent.ConfigAsync(workflowViewConfig));
        exception.Message.ShouldContain("The workflow node agentId not support change.");

        workflowViewConfig.WorkflowNodeList[0].AgentId = fread.GetPrimaryKey();
        await fread.RegisterAsync(moni);
        exception = await Assert.ThrowsAsync<ArgumentException>(async () => await workflowViewAgent.ConfigAsync(workflowViewConfig));
        exception.Message.ShouldContain("already has a parent GAgent");
        
    }
}