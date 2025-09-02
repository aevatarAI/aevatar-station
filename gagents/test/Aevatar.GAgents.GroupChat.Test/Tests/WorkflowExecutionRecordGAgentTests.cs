using Aevatar.Core.Abstractions;
using Aevatar.Core;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.Core.States;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Newtonsoft.Json;
using Shouldly;
using Aevatar.GAgents.InputGAgent.GAgent;
using Aevatar.GAgents.InputGAgent.Dto;
using Volo.Abp;

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
        state.WorkUnitRecords.Count.ShouldBe(1);
        state.WorkUnitRecords.ShouldContain(o => o.WorkUnitGrainId == workerGrainId.ToString());
    }

    [Fact]
    public async Task Handle_GroupChatFinishEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

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

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Running);
        grainARecord.InputData.ShouldBe(JsonConvert.SerializeObject(startExecuteGrain.CoordinatorMessages));
    }

    [Fact]
    public async Task Handle_ChatResponseEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Running);
        grainARecord.InputData.ShouldBe(JsonConvert.SerializeObject(startExecuteGrain.CoordinatorMessages));

        var finishExecuteGrainA = new ChatResponseEvent
        {
            PublisherGrainId = workerGrainId,
            ChatResponse = new ChatResponse
            {
                Content = "Grain response"
            }
        };
        await groupAgent.PublishEventAsync(finishExecuteGrainA);
        await Task.Delay(1000);

        state = await recordAgent.GetStateAsync();
        grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.OutputData.ShouldBe(JsonConvert.SerializeObject(finishExecuteGrainA.ChatResponse.Content));
    }

    [Fact]
    public async Task Handle_ChatResponseEvent_Failure_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(500);

        var failure = new ChatResponseEvent
        {
            PublisherGrainId = workerGrainId,
            FailureSummary = "unit crashed"
        };
        await groupAgent.PublishEventAsync(failure);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        var unit = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        unit.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        unit.FailureSummary.ShouldBe("unit crashed");
        unit.EndTime.ShouldNotBeNull();
    }

    [Fact]
    public async Task IncorrectSequence_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var finishExecuteGrainA = new ChatResponseEvent
        {
            PublisherGrainId = workerGrainId,
            ChatResponse = new ChatResponse
            {
                Content = "Grain response"
            }
        };
        await groupAgent.PublishEventAsync(finishExecuteGrainA);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.OutputData.ShouldBe(JsonConvert.SerializeObject(finishExecuteGrainA.ChatResponse.Content));

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        state = await recordAgent.GetStateAsync();
        grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.InputData.ShouldBe(JsonConvert.SerializeObject(startExecuteGrain.CoordinatorMessages));
    }
    
    private async Task StartExecuteWorkflowAsync(IGroupGAgent groupAgent, GrainId workerGrainId)
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
    }

    [Fact]
    public async Task Member_GetDescription_ShouldIncludeMemberName()
    {
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Alice" });

        var desc = await member.DescribeAsync();
        desc.ShouldContain("Member Name: Alice");
    }

    [Fact]
    public async Task Member_Ping_ShouldPublishPong()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Pingy" });
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(member);
        await group.RegisterAsync(collector);

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new CoordinatorPingEvent { BlackboardId = blackboardId });
        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastPongBlackboardId.ShouldBe(blackboardId);
        s.LastPongMemberName.ShouldBe("Pingy");
        s.LastPongMemberId.ShouldBe(member.GetGrainId().GetGuidKey());
    }

    [Fact]
    public async Task Member_EvaluationInterest_ShouldPublishResponse()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Eva" });
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(member);
        await group.RegisterAsync(collector);

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new EvaluationInterestEvent { BlackboardId = blackboardId, ChatTerm = 123 });
        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastInterestBlackboardId.ShouldBe(blackboardId);
        s.LastInterestMemberId.ShouldBe(member.GetGrainId().GetGuidKey());
        s.LastInterestValue.ShouldBe(77);
        s.LastInterestChatTerm.ShouldBe(123);
    }

    [Fact]
    public async Task Member_GetMessageFromBlackboard_ShouldReturnContent()
    {
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Reader" });

        var blackboardId = Guid.NewGuid();
        var blackboard = await _agentFactory.GetGAgentAsync<IBlackboardGAgent>(blackboardId);
        await blackboard.SetTopic("topic-x");

        var msgs = await member.FetchBlackboardMessages(blackboardId);
        msgs.ShouldNotBeNull();
        msgs.Any(m => m.MessageType == MessageType.BlackboardTopic && m.Content == "topic-x").ShouldBeTrue();
    }

    [Fact]
    public async Task InputGAgent_ChatEvent_SpeakerMatch_ShouldPublishChatResponse()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var input = await _agentFactory.GetGAgentAsync<IInputGAgent>(Guid.NewGuid());
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(input);
        await group.RegisterAsync(collector);

        await input.ConfigAsync(new InputConfigDto { MemberName = "Inny", Input = "Hello" });

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new ChatEvent
        {
            BlackboardId = blackboardId,
            Speaker = input.GetGrainId().GetGuidKey(),
            Term = 1,
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "start" } }
        });

        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastChatBlackboardId.ShouldBe(blackboardId);
        s.LastChatMemberId.ShouldBe(input.GetGrainId().GetGuidKey());
        s.LastChatMemberName.ShouldBe("Inny");
        s.LastChatContent.ShouldBe("Hello");
        s.LastChatTerm.ShouldBe(1);
        s.LastChatFailure.ShouldBeNull();
    }

    [Fact]
    public async Task InputGAgent_ChatEvent_SpeakerMismatch_ShouldBeIgnored()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var input = await _agentFactory.GetGAgentAsync<IInputGAgent>(Guid.NewGuid());
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(input);
        await group.RegisterAsync(collector);

        await input.ConfigAsync(new InputConfigDto { MemberName = "Inny", Input = "Hello" });

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new ChatEvent
        {
            BlackboardId = blackboardId,
            Speaker = Guid.NewGuid(),
            Term = 2,
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "start" } }
        });

        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastChatBlackboardId.ShouldNotBe(blackboardId);
        s.LastChatContent.ShouldBeNull();
    }

    [Fact]
    public async Task InputGAgent_EvaluationInterest_ShouldPublish100()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var input = await _agentFactory.GetGAgentAsync<IInputGAgent>(Guid.NewGuid());
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(input);
        await group.RegisterAsync(collector);

        await input.ConfigAsync(new InputConfigDto { MemberName = "Scorer", Input = "whatever" });

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new EvaluationInterestEvent { BlackboardId = blackboardId, ChatTerm = 9 });
        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastInterestBlackboardId.ShouldBe(blackboardId);
        s.LastInterestMemberId.ShouldBe(input.GetGrainId().GetGuidKey());
        s.LastInterestValue.ShouldBe(100);
        s.LastInterestChatTerm.ShouldBe(9);
    }
    
    [Fact]
    public async Task Workflow_WithExecutionRecord_Failure_ShouldMarkFailed()
    {
        var toni = await _agentFactory.GetGAgentAsync<IFailInputGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new InputConfigDto { MemberName = "Scorer", Input = "whatever" });
        var leader = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await leader.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Leader" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto() { GrainId = toni.GetGrainId().ToString(), NextGrainId = leader.GetGrainId().ToString() },
            new WorkflowUnitDto() { GrainId = leader.GetGrainId().ToString(), NextGrainId = "" }
        };

        var coordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = workflows,
            InitContent = "init",
            EnableExecutionRecord = true
        });

        await groupAgent.RegisterAsync(coordinator);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());
        
        // Wait a bit for first unit to be activated and term to be set
        await Task.Delay(1500);

        var cstate = await coordinator.GetStateAsync();
        cstate.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Failed);
    }

    // Test helper agents for coverage
    [GAgent(nameof(EventCollectorGAgent))]
    public class EventCollectorGAgent : GroupMemberGAgentBase<CollectorState, CollectorLogEvent, EventBase, GroupMemberConfigDto>, IEventCollectorGAgent
    {
        public override Task<string> GetDescriptionAsync() => Task.FromResult("collector");

        protected override Task<int> GetInterestValueAsync(Guid blackboardId) => Task.FromResult(0);

        protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
        {
            return Task.FromResult(new ChatResponse { Content = "noop" });
        }

        [EventHandler]
        public async Task HandleEventAsync(CoordinatorPongEvent @event)
        {
            RaiseEvent(new SetPongLogEvent
            {
                BlackboardId = @event.BlackboardId,
                MemberId = @event.MemberId,
                MemberName = @event.MemberName
            });
            await ConfirmEvents();
        }

        [EventHandler]
        public async Task HandleEventAsync(EvaluationInterestResponseEvent @event)
        {
            RaiseEvent(new SetInterestLogEvent
            {
                BlackboardId = @event.BlackboardId,
                MemberId = @event.MemberId,
                InterestValue = @event.InterestValue,
                ChatTerm = @event.ChatTerm
            });
            await ConfirmEvents();
        }

        [EventHandler]
        public async Task HandleEventAsync(ChatResponseEvent @event)
        {
            RaiseEvent(new SetChatLogEvent
            {
                BlackboardId = @event.BlackboardId,
                MemberId = @event.MemberId,
                MemberName = @event.MemberName,
                Content = @event.ChatResponse?.Content,
                Term = @event.Term,
                Failure = @event.FailureSummary
            });
            await ConfirmEvents();
        }

        protected override void GroupMemberTransitionState(CollectorState state, StateLogEventBase<CollectorLogEvent> @event)
        {
            switch (@event)
            {
                case SetPongLogEvent e:
                    state.LastPongBlackboardId = e.BlackboardId;
                    state.LastPongMemberId = e.MemberId;
                    state.LastPongMemberName = e.MemberName;
                    return;
                case SetInterestLogEvent e2:
                    state.LastInterestBlackboardId = e2.BlackboardId;
                    state.LastInterestMemberId = e2.MemberId;
                    state.LastInterestValue = e2.InterestValue;
                    state.LastInterestChatTerm = e2.ChatTerm;
                    return;
                case SetChatLogEvent e3:
                    state.LastChatBlackboardId = e3.BlackboardId;
                    state.LastChatMemberId = e3.MemberId;
                    state.LastChatMemberName = e3.MemberName;
                    state.LastChatContent = e3.Content;
                    state.LastChatTerm = e3.Term;
                    state.LastChatFailure = e3.Failure;
                    return;
            }
        }
    }

    public interface IEventCollectorGAgent : IStateGAgent<CollectorState>
    {
    }

    [GenerateSerializer]
    public class CollectorLogEvent : StateLogEventBase<CollectorLogEvent>
    {
    }

    [GenerateSerializer]
    public class SetPongLogEvent : CollectorLogEvent
    {
        [Id(0)] public Guid BlackboardId { get; set; }
        [Id(1)] public Guid MemberId { get; set; }
        [Id(2)] public string MemberName { get; set; }
    }

    [GenerateSerializer]
    public class SetInterestLogEvent : CollectorLogEvent
    {
        [Id(0)] public Guid BlackboardId { get; set; }
        [Id(1)] public Guid MemberId { get; set; }
        [Id(2)] public int InterestValue { get; set; }
        [Id(3)] public long ChatTerm { get; set; }
    }

    [GenerateSerializer]
    public class SetChatLogEvent : CollectorLogEvent
    {
        [Id(0)] public Guid BlackboardId { get; set; }
        [Id(1)] public Guid MemberId { get; set; }
        [Id(2)] public string MemberName { get; set; }
        [Id(3)] public string Content { get; set; }
        [Id(4)] public long Term { get; set; }
        [Id(5)] public string Failure { get; set; }
    }

    [GenerateSerializer]
    public class CollectorState : WorkerState
    {
        [Id(10)] public Guid LastPongBlackboardId { get; set; }
        [Id(11)] public Guid LastPongMemberId { get; set; }
        [Id(12)] public string LastPongMemberName { get; set; }
        [Id(13)] public Guid LastInterestBlackboardId { get; set; }
        [Id(14)] public Guid LastInterestMemberId { get; set; }
        [Id(15)] public int LastInterestValue { get; set; }
        [Id(16)] public long LastInterestChatTerm { get; set; }
        [Id(17)] public Guid LastChatBlackboardId { get; set; }
        [Id(18)] public Guid LastChatMemberId { get; set; }
        [Id(19)] public string LastChatMemberName { get; set; }
        [Id(20)] public string LastChatContent { get; set; }
        [Id(21)] public long LastChatTerm { get; set; }
        [Id(22)] public string LastChatFailure { get; set; }
    }

    [GAgent(nameof(TestMemberHelperGAgent))]
    public class TestMemberHelperGAgent : GroupMemberGAgentBase<WorkerState, TestMemberEventLog, EventBase, GroupMemberConfigDto>, ITestMemberHelperGAgent
    {
        protected override Task<int> GetInterestValueAsync(Guid blackboardId) => Task.FromResult(77);

        protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
        {
            return Task.FromResult(new ChatResponse { Content = "ok" });
        }

        public Task<List<ChatMessage>> FetchBlackboardMessages(Guid blackboardId) => GetMessageFromBlackboardAsync(blackboardId);

        public Task<string> DescribeAsync() => GetDescriptionAsync();
    }

    public interface ITestMemberHelperGAgent : IStateGAgent<WorkerState>
    {
        Task<List<ChatMessage>> FetchBlackboardMessages(Guid blackboardId);
        Task<string> DescribeAsync();
    }

    [GenerateSerializer]
    public class TestMemberEventLog : StateLogEventBase<TestMemberEventLog>
    {
    }
    
    [GAgent(nameof(FailInputGAgent))]
    public class FailInputGAgent : InputGAgent.GAgent.InputGAgent, IFailInputGAgent
    {
        protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
        {
            throw new UserFriendlyException("InputGAgent fail");
        }
    }
    
    public interface IFailInputGAgent : IInputGAgent
    {
    }
}