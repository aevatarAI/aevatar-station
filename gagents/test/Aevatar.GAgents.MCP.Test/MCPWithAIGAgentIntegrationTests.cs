using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.MCP.Test;

/// <summary>
/// Test MCP GAgent integration scenarios with AI Agent
/// </summary>
public class MCPWithAIGAgentIntegrationTests : AevatarMCPTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public MCPWithAIGAgentIntegrationTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task MCPGAgent_Should_Work_With_Event_Subscription()
    {
        var mcpConfig = new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "test-integration",
                Command = "test"
            }
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(mcpConfig);

        // Create a simple subscriber GAgent
        var subscriberGAgent = await _gAgentFactory.GetGAgentAsync<ITestSubscriberGAgent>();
        
        // Establish bidirectional subscription relationship
        await mcpGAgent.RegisterAsync(subscriberGAgent);
        await subscriberGAgent.RegisterAsync(mcpGAgent);

        // Act - Publish tool call event through subscriber
        await subscriberGAgent.CallMCPToolAsync();
        
        // Wait for event processing to complete
        await Task.Delay(1000);

        // Assert
        var result = await subscriberGAgent.GetLastResultAsync();
        result.ShouldNotBeNull();
    }
}

public interface ITestSubscriberGAgent : IStateGAgent<TestSubscriberState>
{
    Task CallMCPToolAsync();
    Task<string?> GetLastResultAsync();
}

[GAgent]
public class TestSubscriberGAgent : GAgentBase<TestSubscriberState, TestSubscriberLogEvent>, ITestSubscriberGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test subscriber for MCP events");
    }
    
    protected override void GAgentTransitionState(TestSubscriberState state, StateLogEventBase<TestSubscriberLogEvent> @event)
    {
        switch (@event)
        {
            case TestResultReceivedEvent resultEvent:
                state.LastResult = resultEvent.Result;
                state.LastSuccess = resultEvent.Success;
                break;
        }
    }

    public async Task CallMCPToolAsync()
    {
        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "test-integration",
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object>
            {
                ["param"] = "test"
            }
        };

        await PublishAsync(toolCallEvent);
    }

    [EventHandler]
    public async Task HandleEventAsync(MCPToolResponseEvent @event)
    {
        RaiseEvent(new TestResultReceivedEvent
        {
            Result = @event.Result?.ToString() ?? "Success", 
            Success = @event.Success
        });
        await ConfirmEvents();
    }

    public Task<string?> GetLastResultAsync()
    {
        return Task.FromResult(State.LastResult);
    }
}

[GenerateSerializer]
public class TestSubscriberState : StateBase
{
    [Id(0)] public string? LastResult { get; set; }
    [Id(1)] public bool LastSuccess { get; set; }
}

[GenerateSerializer]
public class TestSubscriberLogEvent : StateLogEventBase<TestSubscriberLogEvent>;

[GenerateSerializer]
public class TestResultReceivedEvent : TestSubscriberLogEvent
{
    [Id(0)] public string Result { get; set; } = string.Empty;
    [Id(1)] public bool Success { get; set; }
}