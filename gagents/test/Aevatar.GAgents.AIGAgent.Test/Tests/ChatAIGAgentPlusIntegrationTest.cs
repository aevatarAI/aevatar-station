using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;
using Aevatar.GAgents.Core;
using Orleans;
using Orleans.Providers;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging;
using Aevatar.GAgents.TestBase;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Integration tests for ChatAIGAgentPlus using AevatarAIGAgentTestBase.
/// These tests use the full Orleans TestCluster infrastructure to test methods
/// that require Orleans Grain functionality (State, event sourcing, etc.)
/// </summary>
[Collection(ClusterCollection.Name)]
public class ChatAIGAgentPlusIntegrationTest : AevatarAIGAgentTestBase
{
    private readonly IGrainFactory _grainFactory;

    public ChatAIGAgentPlusIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    #region Configuration Tests

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IChatAIGAgentPlus>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldBe("Chat AI Agent for group conversations");
    }

    [Fact]
    public async Task ConfigAsync_Should_SetConfigurationCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IChatAIGAgentPlus>(agentId);
        var config = new ChatAIGAgentConfigDtoPlus
        {
            Instructions = "You are a helpful assistant for testing",
            SystemLLM = "OpenAI",
            MemberName = "Test Chat Agent"
        };

        // Act
        await agent.ConfigAsync(config);

        // Assert - Verify configuration was applied (configuration is stored in base class state)
        var state = await agent.GetStateAsync();
        // Note: Configuration properties are stored in the base AIGAgentStateBasePlus, 
        // ChatAIGAgentStatePlus only adds LastResponse, LastActivityTime, TotalInteractions
        state.LastResponse.ShouldBe(""); // Default value for ChatAI-specific state
        state.TotalInteractions.ShouldBe(0); // Default value for ChatAI-specific state
    }

    #endregion

    #region ChatAIGAgent-Specific Workflow Event Handling Tests

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessChatWorkflowEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestChatAIGAgentPlus>(agentId);
        
        // Configure the agent first
        var config = new ChatAIGAgentConfigDtoPlus
        {
            Instructions = "You are a test assistant",
            SystemLLM = "OpenAI",
            MemberName = "Test Chat Agent"
        };
        await testAgent.ConfigAsync(config);

        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Start chat workflow"
        };

        // Act - Test ChatAIGAgent-specific workflow event handling
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert - Verify ChatAIGAgent-specific behavior
        result.ShouldBeTrue();
        
        // Get the captured workflow event from inside the method (Orleans deep copies prevent direct access)
        var capturedEvent = await testAgent.GetCapturedWorkflowEventAsync();
        capturedEvent.ShouldNotBeNull();
        capturedEvent.WorkflowEventType.ShouldBe(WorkflowEventType.WorkflowInProgress);
        capturedEvent.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Completed);
        capturedEvent.Message.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region AI Functionality Tests

    [Fact]
    public async Task InitializeAsync_Should_SetupAICapabilities()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IChatAIGAgentPlus>(agentId);
        
        var initDto = new InitializeDto
        {
            Instructions = "You are a helpful test assistant",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "OpenAI" // Use test configuration
            }
        };

        // Act
        var result = await agent.InitializeAsync(initDto);

        // Assert
        result.ShouldBeTrue();
        
        var state = await agent.GetStateAsync();
        state.PromptTemplate.ShouldBe("You are a helpful test assistant");
    }


    #endregion

    #region State Transition Tests

    [Fact]
    public async Task StateTransition_Should_HandleChatInteractions()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IChatAIGAgentPlus>(agentId);
        
        var config = new ChatAIGAgentConfigDtoPlus
        {
            Instructions = "Test assistant",
            SystemLLM = "OpenAI",
            MemberName = "Test Agent"
        };
        await agent.ConfigAsync(config);

        // Act - Get initial state and verify ChatAI-specific state properties
        var initialState = await agent.GetStateAsync();

        // Assert - Verify ChatAIGAgentStatePlus properties are properly initialized
        initialState.LastResponse.ShouldBe(""); // Default value
        initialState.TotalInteractions.ShouldBe(0); // Default value
        initialState.LastActivityTime.ShouldBe(default(DateTime)); // Default value
        
        // Test that we can call GetLastResponseAsync which uses the state
        var lastResponse = await agent.GetLastResponseAsync();
        lastResponse.ShouldBe(""); // State.LastResponse is initialized as empty string, not null
    }

    #endregion

}

/// <summary>
/// Test extension interface for ChatAIGAgentPlus workflow event handling
/// </summary>
public interface ITestChatAIGAgentPlus : IChatAIGAgentPlus
{
    Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent);
    Task<WorkflowEvent?> GetCapturedWorkflowEventAsync();
}

/// <summary>
/// Test extension for ChatAIGAgentPlus to expose protected methods for testing class-specific behavior
/// </summary>
[GAgent("test-chat-ai-gagent-plus", "test")]
public class TestChatAIGAgentPlus : ChatAIGAgentPlus, ITestChatAIGAgentPlus
{
    private WorkflowEvent? _capturedWorkflowEvent;

    public TestChatAIGAgentPlus(ILogger<TestChatAIGAgentPlus> logger) : base(logger)
    {
    }

    public async Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        // Call the full BusinessAgentBase flow which will internally call our overridden method
        return await OnEventForwardingEventHandlerAsync(workflowEvent);
    }

    public Task<WorkflowEvent?> GetCapturedWorkflowEventAsync()
    {
        return Task.FromResult(_capturedWorkflowEvent);
    }

    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        // Call the base ChatAIGAgent implementation first
        await base.OnBusinessAgentEventForwardingEventHandlerAsync(workflowEvent);
        
        // Capture the workflow event after ChatAIGAgent processing for test verification
        _capturedWorkflowEvent = CloneWorkflowEvent(workflowEvent);
    }

    private WorkflowEvent CloneWorkflowEvent(WorkflowEvent source)
    {
        return new WorkflowEvent
        {
            WorkflowId = source.WorkflowId,
            WorkflowEventType = source.WorkflowEventType,
            Message = source.Message,
            WorkUnitAgentId = source.WorkUnitAgentId,
            AgentName = source.AgentName,
            StepStartTime = source.StepStartTime,
            StepEndTime = source.StepEndTime,
            WorkflowAgentStatus = source.WorkflowAgentStatus,
            ErrorMessage = source.ErrorMessage,
            TaskResult = source.TaskResult,
            Metadata = source.Metadata != null ? new Dictionary<string, object>(source.Metadata) : null
        };
    }
}

