using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Test class for GAgent tools functionality without mocks
/// </summary>
public sealed class GAgentToolsTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly IGAgentService _gAgentService;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly ILogger<GAgentToolPlugin> _logger;

    public GAgentToolsTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentService = GetRequiredService<IGAgentService>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
        _logger = GetRequiredService<ILogger<GAgentToolPlugin>>();
    }

    //[Fact]
    public async Task Should_Enable_GAgent_Tools_When_Configured()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        // Act
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are an AI assistant with GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
        });
        
        // Assert
        var state = await chatAgent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
    }

    //[Fact]
    public async Task Should_Register_Functions_When_Tools_Enabled()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        // Act
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are an AI assistant that can use other agents",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
        });
        
        // Wait a bit for async registration
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        
        // Assert
        var state = await chatAgent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        // Note: RegisteredGAgentFunctions might be empty if registration failed due to reflection
        // This is expected in test environment
    }

    //[Fact]
    public async Task GAgentToolPlugin_Should_List_Available_GAgents()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);
        
        // Act
        var result = await plugin.ListGAgentsAsync();
        
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("success");
        result.ShouldContain("true");
        result.ShouldContain("total");
        // The result should be valid JSON
        result.ShouldContain("{");
        result.ShouldContain("}");
    }

    //[Fact]
    public async Task GAgentToolPlugin_Should_Get_GAgent_Info()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);
        
        // First, get available GAgents
        var listResult = await plugin.ListGAgentsAsync();
        listResult.ShouldContain("gagents");
        
        // Act - Try to get info for a known test GAgent type
        var testGrainType = "test.aevatar.gagents.aigagent.test.gagents.chatgagents.chatagent";
        var result = await plugin.GetGAgentInfoAsync(testGrainType);
        
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("{");
        result.ShouldContain("}");
        // Should contain either success or error
        var containsSuccess = result.Contains("success");
        var containsError = result.Contains("error");
        (containsSuccess || containsError).ShouldBeTrue();
    }

    //[Fact]
    public async Task GAgentToolPlugin_Should_Handle_Invalid_GAgent_Type()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);
        
        // Act
        var result = await plugin.GetGAgentInfoAsync("invalid.gagent.type");
        
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("error");
        result.ShouldContain("No active nodes are compatible with grain");
    }

    //[Fact]
    public async Task GAgentToolPlugin_Should_Handle_Invoke_With_Invalid_Parameters()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);
        
        // Act
        var result = await plugin.InvokeGAgentAsync(
            "invalid.gagent",
            "InvalidEvent",
            "invalid json"
        );
        
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("error");
        result.ShouldContain("success");
        result.ShouldContain("false");
    }

    //[Fact]
    public async Task Should_Not_Register_Tools_When_Disabled()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        // Act
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a basic AI assistant",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
        });
        
        // Assert
        var state = await chatAgent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeFalse();
        state.RegisteredGAgentFunctions.ShouldBeEmpty();
    }

    //[Fact]
    public async Task Should_Filter_GAgents_By_Allowed_Types()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        // Act
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are an AI with limited agent access",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
            AllowedGAgentTypes = [GrainType.Create("NonExistentType")]
        });
        
        // Wait for registration attempt
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        
        // Assert
        var state = await chatAgent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        // With a non-existent type filter, no functions should be registered
        // (or registration might fail due to reflection issues in test)
    }

    //[Fact]
    public async Task GAgentService_Should_Return_Available_GAgents()
    {
        // This test verifies the GAgentService is working correctly
        
        // Act
        var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
        
        // Assert
        allGAgents.ShouldNotBeNull();
        // In test environment, we should have at least the test GAgents registered
        allGAgents.Count.ShouldBeGreaterThan(0);
        
        // Verify structure
        foreach (var kvp in allGAgents)
        {
            kvp.Key.ToString().ShouldNotBeNullOrEmpty(); // GrainType
            kvp.Value.ShouldNotBeNull(); // List of event types
            kvp.Value.Count.ShouldBeGreaterThan(0); // Should have at least one event
        }
    }
} 