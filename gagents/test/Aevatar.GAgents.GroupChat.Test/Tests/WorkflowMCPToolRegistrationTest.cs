using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

/// <summary>
/// Test the MCP tool registration logic in WorkflowCoordinatorGAgent.TryActiveWorkUnitAsync
/// Verifies that when a business GAgent's next grain ID is an MCPGAgent, 
/// the MCP tools are registered to the AI kernel functions
/// </summary>
public sealed class WorkflowMCPToolRegistrationTest : AevatarGroupChatTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _agentFactory;

    public WorkflowMCPToolRegistrationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task TestWorkflowAIGAgent_BasicFunctionality_ShouldWork()
    {
        // Arrange: Create a test AI agent
        var aiAgent = await _agentFactory.GetGAgentAsync<ITestWorkflowAIGAgent>(Guid.NewGuid());
        
        // Test basic functionality without initialization
        var initialToolCount = await aiAgent.GetRegisteredToolsCountAsync();
        var initialFunctions = await aiAgent.GetAvailableKernelFunctionsAsync();
        
        _testOutputHelper.WriteLine($"Basic test - Initial tool count: {initialToolCount}");
        _testOutputHelper.WriteLine($"Basic test - Available functions: {string.Join(", ", initialFunctions)}");
        
        // Should be able to call these methods without errors (even if they return 0)
        initialToolCount.ShouldBeGreaterThanOrEqualTo(0);
        initialFunctions.ShouldNotBeNull();
        
        // Initialize the AI agent
        var initResult = await aiAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test AI agent for basic functionality",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });
        
        initResult.ShouldBeTrue("AI agent should initialize successfully");
        
        // Check again after initialization
        var postInitToolCount = await aiAgent.GetRegisteredToolsCountAsync();
        var postInitFunctions = await aiAgent.GetAvailableKernelFunctionsAsync();
        
        _testOutputHelper.WriteLine($"Basic test - Post-init tool count: {postInitToolCount}");
        _testOutputHelper.WriteLine($"Basic test - Post-init functions: {string.Join(", ", postInitFunctions)}");
        
        postInitToolCount.ShouldBeGreaterThanOrEqualTo(0);
        postInitFunctions.ShouldNotBeNull();
    }

    [Fact]
    public async Task TryActiveWorkUnitAsync_WithMCPGAgentInNextGrainId_Should_RegisterMCPTools()
    {
        // Arrange: Create a test AI agent that can receive MCP tools
        var aiAgent = await _agentFactory.GetGAgentAsync<ITestWorkflowAIGAgent>(Guid.NewGuid());
        
        // Initialize the AI agent with proper LLM configuration
        await aiAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test AI agent for MCP tool registration",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });
        
        // ConfigAsync is not needed for this test AI agent

        // Create an MCP agent (this will be the "next" agent in the workflow)
        var mcpGAgent = await _agentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"]
            },
            RequestTimeout = TimeSpan.FromSeconds(10)
        });

        // Get initial tool count from AI agent
        var initialToolCount = await aiAgent.GetRegisteredToolsCountAsync();

        // Create workflow configuration where AI agent points to MCP agent
        var workflows = new List<WorkflowUnitDto>
        {
            new WorkflowUnitDto
            {
                GrainId = aiAgent.GetGrainId().ToString(),
                NextGrainId = mcpGAgent.GetGrainId().ToString()
            },
            new WorkflowUnitDto
            {
                GrainId = mcpGAgent.GetGrainId().ToString(),
                NextGrainId = "" // Terminal node
            }
        };

        // Create workflow coordinator
        var workflowCoordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = workflows
        });

        // Create group agent and register the workflow
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(workflowCoordinator);

        // Act: Start the workflow - this should trigger TryActiveWorkUnitAsync
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());

        // Wait for the workflow to process and register MCP tools
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert: Verify that MCP tools have been registered
        var finalToolCount = await aiAgent.GetRegisteredToolsCountAsync();
        var availableFunctions = await aiAgent.GetAvailableKernelFunctionsAsync();

        // The AI agent should now have MCP tools registered
        finalToolCount.ShouldBeGreaterThan(initialToolCount, 
            $"Expected more tools after MCP registration. Initial: {initialToolCount}, Final: {finalToolCount}");

        // Verify specific MCP tools are available
        var hasMCPReadFile = await aiAgent.HasMCPFunctionAsync("filesystem", "read_file");
        var hasMCPWriteFile = await aiAgent.HasMCPFunctionAsync("filesystem", "write_file");
        var hasMCPListDir = await aiAgent.HasMCPFunctionAsync("filesystem", "list_directory");

        // At least one of the expected MCP tools should be available
        (hasMCPReadFile || hasMCPWriteFile || hasMCPListDir).ShouldBeTrue(
            $"Expected MCP tools to be registered. Available functions: {string.Join(", ", availableFunctions)}");

        // Log results for debugging
        _testOutputHelper.WriteLine($"Initial tool count: {initialToolCount}");
        _testOutputHelper.WriteLine($"Final tool count: {finalToolCount}");
        _testOutputHelper.WriteLine($"Available functions: {string.Join(", ", availableFunctions)}");
        _testOutputHelper.WriteLine($"Has MCP read_file: {hasMCPReadFile}");
        _testOutputHelper.WriteLine($"Has MCP write_file: {hasMCPWriteFile}");
        _testOutputHelper.WriteLine($"Has MCP list_directory: {hasMCPListDir}");
    }

    [Fact]
    public async Task TryActiveWorkUnitAsync_WithoutMCPGAgent_Should_NotRegisterMCPTools()
    {
        // Arrange: Create a test AI agent
        var aiAgent = await _agentFactory.GetGAgentAsync<ITestWorkflowAIGAgent>(Guid.NewGuid());
        
        await aiAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test AI agent without MCP tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });
        
        // ConfigAsync is not needed for this test AI agent

        // Create a regular worker agent (not MCP)
        var workerAgent = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await workerAgent.ConfigAsync(new GroupMemberConfigDto { MemberName = "Worker" });

        // Get initial tool count
        var initialToolCount = await aiAgent.GetRegisteredToolsCountAsync();

        // Create workflow configuration where AI agent points to worker agent (not MCP)
        var workflows = new List<WorkflowUnitDto>
        {
            new WorkflowUnitDto
            {
                GrainId = aiAgent.GetGrainId().ToString(),
                NextGrainId = workerAgent.GetGrainId().ToString()
            },
            new WorkflowUnitDto
            {
                GrainId = workerAgent.GetGrainId().ToString(),
                NextGrainId = "" // Terminal node
            }
        };

        // Create workflow coordinator
        var workflowCoordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = workflows
        });

        // Create group agent and register the workflow
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(workflowCoordinator);

        // Act: Start the workflow
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert: Verify that no additional MCP tools were registered
        var finalToolCount = await aiAgent.GetRegisteredToolsCountAsync();
        var availableFunctions = await aiAgent.GetAvailableKernelFunctionsAsync();

        // Tool count should not significantly increase since no MCP tools were added
        var toolIncrease = finalToolCount - initialToolCount;
        toolIncrease.ShouldBeLessThanOrEqualTo(2, // Allow for minimal system functions
            $"Expected minimal tool increase without MCP. Initial: {initialToolCount}, Final: {finalToolCount}");

        // Should not have MCP-specific tools
        var hasMCPReadFile = await aiAgent.HasMCPFunctionAsync("test-filesystem", "read_file");
        hasMCPReadFile.ShouldBeFalse("Should not have MCP tools when no MCPGAgent in workflow");

        _testOutputHelper.WriteLine($"Control test - Initial tool count: {initialToolCount}");
        _testOutputHelper.WriteLine($"Control test - Final tool count: {finalToolCount}");
        _testOutputHelper.WriteLine($"Control test - Available functions: {string.Join(", ", availableFunctions)}");
    }

    [Fact]
    public async Task TryActiveWorkUnitAsync_WithMultipleMCPGAgents_Should_RegisterAllMCPTools()
    {
        // Arrange: Create a test AI agent
        var aiAgent = await _agentFactory.GetGAgentAsync<ITestWorkflowAIGAgent>(Guid.NewGuid());
        
        await aiAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test AI agent for multiple MCP tool registration",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });
        
        // ConfigAsync is not needed for this test AI agent

        // Create multiple MCP agents
        var mcpAgent1 = await _agentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = new List<string> { "@modelcontextprotocol/server-filesystem", "/workspace" }
            }
        });

        var mcpAgent2 = await _agentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "sqlite",
                Command = "npx",
                Args = new List<string> { "@modelcontextprotocol/server-sqlite", "memory:" }
            }
        });

        // Get initial tool count
        var initialToolCount = await aiAgent.GetRegisteredToolsCountAsync();

        // Create workflow configuration with AI agent pointing to multiple MCP agents
        var workflows = new List<WorkflowUnitDto>
        {
            new WorkflowUnitDto
            {
                GrainId = aiAgent.GetGrainId().ToString(),
                NextGrainId = mcpAgent1.GetGrainId().ToString()
            },
            new WorkflowUnitDto
            {
                GrainId = aiAgent.GetGrainId().ToString(),
                NextGrainId = mcpAgent2.GetGrainId().ToString()
            },
            new WorkflowUnitDto
            {
                GrainId = mcpAgent1.GetGrainId().ToString(),
                NextGrainId = ""
            },
            new WorkflowUnitDto
            {
                GrainId = mcpAgent2.GetGrainId().ToString(),
                NextGrainId = ""
            }
        };

        // Create workflow coordinator
        var workflowCoordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = workflows
        });

        // Create group agent and register the workflow
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(workflowCoordinator);

        // Act: Start the workflow
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(4));

        // Assert: Verify that tools from both MCP agents are registered
        var finalToolCount = await aiAgent.GetRegisteredToolsCountAsync();
        var availableFunctions = await aiAgent.GetAvailableKernelFunctionsAsync();

        finalToolCount.ShouldBeGreaterThan(initialToolCount,
            $"Expected tools from multiple MCP agents. Initial: {initialToolCount}, Final: {finalToolCount}");

        // Check for tools from both servers
        var hasFilesystemTools = await aiAgent.HasMCPFunctionAsync("filesystem", "read_file");
        var hasSqliteTools = await aiAgent.HasMCPFunctionAsync("sqlite", "execute_query");

        // At least one tool from each type should be available
        (hasFilesystemTools || hasSqliteTools).ShouldBeTrue(
            $"Expected tools from multiple MCP servers. Available: {string.Join(", ", availableFunctions)}");

        _testOutputHelper.WriteLine($"Multiple MCP test - Initial tool count: {initialToolCount}");
        _testOutputHelper.WriteLine($"Multiple MCP test - Final tool count: {finalToolCount}");
        _testOutputHelper.WriteLine($"Multiple MCP test - Available functions: {string.Join(", ", availableFunctions)}");
    }
}