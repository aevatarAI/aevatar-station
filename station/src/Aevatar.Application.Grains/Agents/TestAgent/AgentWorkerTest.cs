using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Orleans.Providers;
using Volo.Abp;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentWorkerTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentWorkerTest: GroupMemberGAgentBase<AgentWorkerTestState, AgentWorkerTestEventLog, EventBase, AgentWorkerTestConfigDto>, IAgentWorkerTest
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("test Worker GAgent");
    }

    public Task<AgentWorkerTestState> GetState()
    {
        return Task.FromResult(State);
    }

    protected override async Task PerformConfigAsync(AgentWorkerTestConfigDto configuration)
    {
        if (!configuration.FailureSummary.IsNullOrEmpty())
        {
            RaiseEvent(new AgentWorkerTestFailureLogEvent
            {
                FailureSummary = configuration.FailureSummary
            });
            await ConfirmEvents();
        }
        await base.PerformConfigAsync(configuration);
    }

    protected override Task<int> GetInterestValueAsync()
    {
        var random = new Random();

        return Task.FromResult(random.Next(1, 90));
    }
    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] {WorkflowIdProperty, RoundIdProperty, GrainIdProperty})]
    protected override async Task<ChatResponse> ChatAsync(List<GroupChat.GAgent.Feature.Common.ChatMessage>? coordinatorMessages)
    {
        if (!State.FailureSummary.IsNullOrEmpty())
        {
            throw new UserFriendlyException(State.FailureSummary);
        }

        var response = new ChatResponse();
        response.Content = $"{State.MemberName} Send the message";
        return response;
    }
    
    protected override void GroupMemberTransitionState(AgentWorkerTestState testState, StateLogEventBase<AgentWorkerTestEventLog> @event)
    {
        switch (@event)
        {
            case AgentWorkerTestFailureLogEvent workerFailureLogEvent:
                testState.FailureSummary = workerFailureLogEvent.FailureSummary;
                return;
        }
    }
}

[GenerateSerializer]
public class AgentWorkerTestEventLog : StateLogEventBase<AgentWorkerTestEventLog>
{
}

[GenerateSerializer]
public class AgentWorkerTestFailureLogEvent : AgentWorkerTestEventLog
{
    [Id(0)] public string FailureSummary;
}


public interface IAgentWorkerTest : IStateGAgent<AgentWorkerTestState>
{
}

[GenerateSerializer]
public class AgentWorkerTestState : GroupMemberState
{
    [Id(0)] public string FailureSummary { get; set; }
}

[GenerateSerializer]
public class AgentWorkerTestConfigDto : GroupMemberConfigDto
{
    [Id(0)] 
    public string FailureSummary { get; set; } = "Fail";
}