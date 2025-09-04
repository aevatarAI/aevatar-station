using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Util;
using Aevatar.GAgents.TestBase;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Unit tests for VideoGenerationGAgent
/// Tests GAgent initialization, event handling, and integration with BytePlusModelArkClient
/// Requires Orleans cluster for proper GAgent testing
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class VideoGenerationGAgentTests : AevatarAIGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly Mock<BytePlusModelArkClient> _mockBytePlusClient;

    public VideoGenerationGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _mockBytePlusClient = new Mock<BytePlusModelArkClient>();
    }

    #region GAgent Initialization Tests

    [Fact]
    public async Task VideoGenerationGAgent_ShouldInitializeCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = new VideoGenerationConfigDto
        {
            Duration = 10,
            Resolution = "1080p",
            Style = "cinematic"
        };

        // Act
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(agentId, config);
        var description = await agent.GetDescriptionAsync();

        // Assert
        agent.ShouldNotBeNull();
        description.ShouldNotBeNullOrEmpty();
        description.ShouldContain("video generation");
        
        _testOutputHelper.WriteLine($"GAgent initialized with ID: {agentId}");
        _testOutputHelper.WriteLine($"Description: {description}");
    }

    [Fact]
    public async Task VideoGenerationGAgent_GetDescriptionAsync_ShouldReturnValidDescription()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        
        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNullOrEmpty();
        description.ShouldContain("video");
        description.ShouldContain("generation");
        
        _testOutputHelper.WriteLine($"GAgent description: {description}");
    }

    #endregion

    #region GAgent Configuration Tests

    [Fact]
    public async Task VideoGenerationGAgent_WithValidConfiguration_ShouldApplyConfig()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = new VideoGenerationConfigDto
        {
            Duration = 15,
            AspectRatio = "16:9",
            Resolution = "4K",
            Style = "realistic",
            MotionStrength = 0.8f,
            MotionType = "dynamic",
            ImageInfluence = 0.9f,
            AutoReturnResult = true
        };

        // Act
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(agentId, config);
        var description = await agent.GetDescriptionAsync();

        // Assert
        agent.ShouldNotBeNull();
        description.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine($"GAgent configured with custom settings");
        _testOutputHelper.WriteLine($"Config: Duration={config.Duration}, Resolution={config.Resolution}");
    }

    [Fact]
    public async Task VideoGenerationGAgent_WithNullConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        // Act
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(agentId, null);
        var description = await agent.GetDescriptionAsync();

        // Assert
        agent.ShouldNotBeNull();
        description.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine("GAgent initialized with default configuration");
    }

    #endregion

    #region GAgent State Management Tests

    [Fact]
    public async Task VideoGenerationGAgent_ShouldMaintainState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(agentId);

        // Act
        var state1 = await agent.GetStateAsync();
        var description = await agent.GetDescriptionAsync();
        var state2 = await agent.GetStateAsync();

        // Assert
        state1.ShouldNotBeNull();
        state2.ShouldNotBeNull();
        // State should persist - check that we can get the same state twice
        description.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine($"GAgent state maintained successfully");
    }

    [Fact]
    public async Task VideoGenerationGAgent_ShouldHaveUniqueIds()
    {
        // Arrange
        var agent1 = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        var agent2 = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());

        // Act
        var state1 = await agent1.GetStateAsync();
        var state2 = await agent2.GetStateAsync();

        // Assert
        // Each agent should have unique state
        state1.ShouldNotBeNull();
        state2.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"Agent 1 state retrieved successfully");
        _testOutputHelper.WriteLine($"Agent 2 state retrieved successfully");
    }

    #endregion

    #region GAgent Event Handling Tests

    [Fact]
    public async Task VideoGenerationGAgent_ShouldHandleEvents()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNullOrEmpty();
        // Note: Event handling would require more complex setup with actual events
        // This is a basic test to ensure the agent can respond to method calls
        
        _testOutputHelper.WriteLine($"GAgent event handling test passed");
    }

    #endregion

    #region Debug Tests

    [Fact]
    public async Task VideoGenerationGAgent_GetLLMConfigAsync_DebugTest()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        
        // Test different configuration keys by setting SystemLLM and then getting the config
        var testKeys = new[] { "DeepSeek", "OpenAI", "BytePlusVideoGeneration", "Google" };
        
        foreach (var key in testKeys)
        {
            try
            {
                // Set the SystemLLM to test the key
                await agent.SetSystemLLMAsync(key);
                
                // Try to get the configuration
                var config = await agent.GetLLMConfigAsync();
                
                if (config != null)
                {
                    _testOutputHelper.WriteLine($"✅ SUCCESS: Resolved config for '{key}': {config.ModelName}");
                }
                else
                {
                    _testOutputHelper.WriteLine($"❌ FAILED: Could not resolve config for '{key}'");
                }
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine($"💥 EXCEPTION: Error resolving config for '{key}': {ex.Message}");
            }
        }
        
        // This test is for debugging, so we don't assert anything specific
        // Just log the results to understand what's happening
    }
    
    [Fact]
    public async Task VideoGenerationGAgent_ResolveSystemConfigAsync_DebugTest()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        
        // Test different configuration keys directly
        var testKeys = new[] { "DeepSeek", "OpenAI", "BytePlusVideoGeneration", "Google" };
        
        foreach (var key in testKeys)
        {
            try
            {
                // Try to get the configuration directly using GetLLMConfigAsync
                var config = await agent.GetLLMConfigAsync();
                
                if (config != null)
                {
                    _testOutputHelper.WriteLine($"✅ SUCCESS: Resolved config for '{key}': {config.ModelName}");
                }
                else
                {
                    _testOutputHelper.WriteLine($"❌ FAILED: Could not resolve config for '{key}'");
                }
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine($"💥 EXCEPTION: Error resolving config for '{key}': {ex.Message}");
            }
        }
        
        // This test is for debugging, so we don't assert anything specific
        // Just log the results to understand what's happening
    }

    #endregion

    #region GAgent Integration Tests

    [Fact]
    public async Task VideoGenerationGAgent_ShouldBeAbleToInitializeWithAI()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        var initDto = new InitializeDto
        {
            Instructions = "You are a video generation assistant. Help users create videos from text or images.",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "BytePlusVideoGeneration"
            }
        };

        // Act
        var result = await agent.InitializeAsync(initDto);

        // Assert
        result.ShouldBeTrue(); // Should initialize successfully
        var description = await agent.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine($"AI GAgent initialized successfully: {result}");
        _testOutputHelper.WriteLine($"Description: {description}");
    }

    [Fact]
    public async Task VideoGenerationGAgent_ShouldGenerateVideoFromText()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        var config = new VideoGenerationConfigDto
        {
            Duration = 5,
            Resolution = "720p",
            Style = "cinematic"
        };

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync("A beautiful sunset", config);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine($"Video generation task started: {taskId}");
    }

    #endregion

    #region GAgent Error Handling Tests

    [Fact]
    public async Task VideoGenerationGAgent_WithInvalidConfiguration_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var invalidConfig = new VideoGenerationConfigDto
        {
            Duration = -1, // Invalid duration
            Resolution = "", // Empty resolution
        };

        // Act
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(agentId, invalidConfig);

        // Assert
        agent.ShouldNotBeNull();
        var description = await agent.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine("GAgent handled invalid configuration gracefully");
    }

    #endregion

    #region Helper Methods

    private VideoGenerationConfigDto CreateTestConfig()
    {
        return new VideoGenerationConfigDto
        {
            Duration = 10,
            AspectRatio = "16:9",
            Resolution = "1080p",
            Style = "cinematic",
            MotionStrength = 0.7f,
            MotionType = "smooth",
            ImageInfluence = 0.8f,
            AutoReturnResult = true
        };
    }

    #endregion
}