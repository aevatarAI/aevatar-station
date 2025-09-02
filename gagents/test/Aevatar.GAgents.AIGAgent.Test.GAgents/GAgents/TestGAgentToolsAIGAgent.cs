using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Test.TestAgents.Events;

namespace Aevatar.GAgents.AIGAgent.Test.TestAgents;

public interface ITestGAgentToolsAIGAgent : IAIGAgent, IStateGAgent<TestGAgentToolsAIGAgentState>
{
    Task<List<ToolCallDetail>> GetCurrentToolCallsAsync();
    Task ClearToolCallsAsync();
}

[GenerateSerializer]
public class TestMCPAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> TestMessages { get; set; } = new();
    [Id(1)] public int TestEventCount { get; set; } = 0;
    [Id(2)] public Dictionary<string, object> TestData { get; set; } = new();
}

[GenerateSerializer]
public class TestGAgentToolsAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> TestMessages { get; set; } = new();
    [Id(1)] public int TestEventCount { get; set; } = 0;
    [Id(2)] public Dictionary<string, object> TestData { get; set; } = new();
}

[GAgent]
public class TestGAgentToolsAIGAgent :
    AIGAgentBase<TestGAgentToolsAIGAgentState, TestGAgentToolsAIGAgentStateLogEvent, TestGAgentToolsAIGAgentEvent,
        ConfigurationBase>,
    ITestGAgentToolsAIGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        return "Test GAgent Tools AIGAgent for unit testing";
    }

    public async Task<List<ToolCallDetail>> GetCurrentToolCallsAsync()
    {
        return CurrentToolCalls.ToList();
    }

    public async Task ClearToolCallsAsync()
    {
        ClearToolCalls();
    }

    [EventHandler]
    public async Task HandleTestEvent(TestGAgentToolsAIGAgentEvent testEvent)
    {
        var logEvent = new TestGAgentToolsAIGAgentStateLogEvent
        {
            PromptTemplate = testEvent.Message
        };

        RaiseEvent(logEvent);
        await ConfirmEvents();
    }

    protected override void AIGAgentTransitionState(TestGAgentToolsAIGAgentState state,
        StateLogEventBase<TestGAgentToolsAIGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case TestGAgentToolsAIGAgentStateLogEvent logEvent:
                state.PromptTemplate = logEvent.PromptTemplate;
                state.SystemLLM = logEvent.SystemLLM;
                state.LLMConfigKey = logEvent.LLMConfigKey;
                state.EnableGAgentTools = logEvent.EnableGAgentTools;
                state.TestMessages = logEvent.TestMessages;
                state.TestData = logEvent.TestData;
                break;
        }
    }
}