using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using System.Text.Json;
using Shouldly;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

public class WorkflowExecutionRecordGAgentTests : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public WorkflowExecutionRecordGAgentTests()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task Handle_StartExecuteWorkflowEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        var startExecuteWorkflowEvent = new StartExecuteWorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            RoundId = 100,
            Content = "Init",
            WorkUnitInfos = new List<WorkUnitInfo>
            {
                new WorkUnitInfo
                {
                    GrainId = workerGrainId.ToString(), NextGrainId = "", UnitStatusEnum = WorkerUnitStatusEnum.Pending
                }
            }
        };

        await groupAgent.PublishEventAsync(startExecuteWorkflowEvent);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.WorkflowId.ShouldBe(startExecuteWorkflowEvent.WorkflowId);
        state.RoundId.ShouldBe(startExecuteWorkflowEvent.RoundId);
        state.InitContent.ShouldBe(startExecuteWorkflowEvent.Content);
        state.Status.ShouldBe(WorkflowExecutionStatus.Running);
        state.WorkUnitInfos.Count.ShouldBe(1);
        state.WorkUnitInfos.ShouldContain(o => o.GrainId == workerGrainId.ToString());
        // Records are now created dynamically when StartExecuteWorkUnitEvent is received
        state.WorkUnitRecords.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_GroupChatFinishEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        var workflowId = await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var finishExecuteWorkflowEvent = new GroupChatFinishEvent
        {
        };

        await groupAgent.PublishEventAsync(finishExecuteWorkflowEvent);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.Status.ShouldBe(WorkflowExecutionStatus.Completed);
    }

    [Fact]
    public async Task Handle_StartExecuteWorkUnitEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        var workflowId = await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            TargetAgentId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.TargetAgentId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Running);
        grainARecord.InputData.ShouldBe(JsonSerializer.Serialize(startExecuteGrain.CoordinatorMessages));
    }

    [Fact]
    public async Task Handle_ChatResponseEvent_Test()
    {
        var workerGuid = Guid.NewGuid();
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(workerGuid);
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        var workflowId = await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            TargetAgentId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.TargetAgentId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Running);
        grainARecord.InputData.ShouldBe(JsonSerializer.Serialize(startExecuteGrain.CoordinatorMessages));

        var finishExecuteGrainA = new ChatResponseEvent
        {
            BlackboardId = workflowId,
            MemberId = workerGuid,
            PublisherGrainId = workerGrainId,
            ChatResponse = new ChatResponse
            {
                Content = "Grain response"
            }
        };
        await groupAgent.PublishEventAsync(finishExecuteGrainA);
        await Task.Delay(1000);

        state = await recordAgent.GetStateAsync();
        grainARecord = state.WorkUnitRecords.First(o => o.TargetAgentId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.OutputData.ShouldBe(JsonSerializer.Serialize(finishExecuteGrainA.ChatResponse.Content));
    }

    [Fact]
    public async Task IncorrectSequence_Test()
    {
        var workerGuid = Guid.NewGuid();
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(workerGuid);
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        var workflowId = await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var finishExecuteGrainA = new ChatResponseEvent
        {
            BlackboardId = workflowId,
            MemberId = workerGuid,
            PublisherGrainId = workerGrainId,
            ChatResponse = new ChatResponse
            {
                Content = "Grain response"
            }
        };
        await groupAgent.PublishEventAsync(finishExecuteGrainA);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.TargetAgentId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.OutputData.ShouldBe(JsonSerializer.Serialize(finishExecuteGrainA.ChatResponse.Content));

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            TargetAgentId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        state = await recordAgent.GetStateAsync();
        grainARecord = state.WorkUnitRecords.First(o => o.TargetAgentId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.InputData.ShouldBe(JsonSerializer.Serialize(startExecuteGrain.CoordinatorMessages));
    }

    private async Task<Guid> StartExecuteWorkflowAsync(IGroupGAgent groupAgent, GrainId workerGrainId)
    {
        var startExecuteWorkflowEvent = new StartExecuteWorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            RoundId = 100,
            Content = "Init",
            WorkUnitInfos = new List<WorkUnitInfo>
            {
                new WorkUnitInfo
                {
                    GrainId = workerGrainId.ToString(), NextGrainId = "", UnitStatusEnum = WorkerUnitStatusEnum.Pending
                }
            }
        };

        await groupAgent.PublishEventAsync(startExecuteWorkflowEvent);
        await Task.Delay(1000);
        return startExecuteWorkflowEvent.WorkflowId;
    }
}