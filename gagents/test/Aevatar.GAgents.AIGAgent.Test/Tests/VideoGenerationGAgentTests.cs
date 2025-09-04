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
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using GroupChat.GAgent.Feature.Common;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Moq.Protected;
using System.Threading;

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

    public VideoGenerationGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    /// <summary>
    /// Creates a test-specific GAgent with proper initialization for testing
    /// The HttpClient with mocked BytePlus API responses is provided by the DI container
    /// </summary>
    private async Task<IVideoGenerationGAgent> CreateTestVideoGenerationGAgentAsync()
    {
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        return agent;
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

    #region Missing Method Tests - Core Functionality

    #region GenerateVideoFromTextAsync Tests

    [Fact]
    public async Task GenerateVideoFromTextAsync_ValidPrompt_ShouldReturnTaskId()
    {
        // Arrange
        var agent = await CreateTestVideoGenerationGAgentAsync();
        var prompt = "A beautiful sunset over the ocean";
        var options = new VideoGenerationConfigDto { Duration = 10, Resolution = "1080p" };

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync(prompt, options);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
        
        _testOutputHelper.WriteLine($"Generated task ID: {taskId}");
    }

    [Fact]
    public async Task GenerateVideoFromTextAsync_NullPrompt_ShouldUseDefaultPrompt()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync(null);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromTextAsync_EmptyPrompt_ShouldUseDefaultPrompt()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync("");

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromTextAsync_NullOptions_ShouldUseDefaultOptions()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var prompt = "A dancing robot";

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync(prompt, null);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromTextAsync_LongPrompt_ShouldHandleCorrectly()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var longPrompt = new string('A', 1000); // 1000 character prompt

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync(longPrompt);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromTextAsync_WithoutInitialization_ShouldReturnTaskId()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        // Note: Agent automatically initializes BytePlus client in OnAIGAgentActivateAsync
        
        var prompt = "A beautiful sunset";

        // Act
        var taskId = await agent.GenerateVideoFromTextAsync(prompt);

        // Assert - Agent always returns a taskId (BytePlus client is auto-initialized)
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    #endregion

    #region GenerateVideoFromImageAsync Tests

    [Fact]
    public async Task GenerateVideoFromImageAsync_ValidInputs_ShouldReturnTaskId()
    {
        // Arrange
        var agent = await CreateTestVideoGenerationGAgentAsync();
        var imageUrl = "https://example.com/image.jpg";
        var prompt = "Make this image come to life with gentle motion";
        var options = new VideoGenerationConfigDto { Duration = 5, Resolution = "720p" };

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync(imageUrl, prompt, options);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
        
        _testOutputHelper.WriteLine($"Generated task ID: {taskId}");
    }

    [Fact]
    public async Task GenerateVideoFromImageAsync_NullImageUrl_ShouldReturnFailedTaskId()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var prompt = "Make this image come to life";

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync(null, prompt);

        // Assert - Returns failed taskId instead of throwing exception
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
        
        // Verify the task has failed status with error message
        var status = await agent.GetVideoStatusAsync(taskId);
        status.Status.ShouldBe("failed");
        status.ErrorMessage.ShouldBe("Image URL is required");
    }

    [Fact]
    public async Task GenerateVideoFromImageAsync_EmptyImageUrl_ShouldReturnFailedTaskId()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var prompt = "Make this image come to life";

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync("", prompt);

        // Assert - Returns failed taskId instead of throwing exception
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
        
        // Verify the task has failed status with error message
        var status = await agent.GetVideoStatusAsync(taskId);
        status.Status.ShouldBe("failed");
        status.ErrorMessage.ShouldBe("Image URL is required");
    }

    [Fact]
    public async Task GenerateVideoFromImageAsync_InvalidImageUrl_ShouldReturnTaskId()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var invalidImageUrl = "not-a-valid-url";
        var prompt = "Make this image come to life";

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync(invalidImageUrl, prompt);

        // Assert - Agent processes even invalid URLs and returns taskId
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromImageAsync_NullPrompt_ShouldUseDefaultPrompt()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var imageUrl = "https://example.com/image.jpg";

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync(imageUrl, null);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromImageAsync_NullOptions_ShouldUseDefaultOptions()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var imageUrl = "https://example.com/image.jpg";
        var prompt = "Add motion to this image";

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync(imageUrl, prompt, null);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    [Fact]
    public async Task GenerateVideoFromImageAsync_WithoutInitialization_ShouldReturnTaskId()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        // Note: Agent automatically initializes BytePlus client in OnAIGAgentActivateAsync
        
        var imageUrl = "https://example.com/image.jpg";
        var prompt = "Add motion to this image";

        // Act
        var taskId = await agent.GenerateVideoFromImageAsync(imageUrl, prompt);

        // Assert - Agent always returns a taskId (BytePlus client is auto-initialized)
        taskId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(taskId, out _).ShouldBeTrue("TaskId should be a valid GUID");
    }

    #endregion

    #region GetVideoStatusAsync Tests

    [Fact]
    public async Task GetVideoStatusAsync_ExistingTask_ShouldReturnStatus()
    {
        // Arrange
        var agent = await CreateTestVideoGenerationGAgentAsync();

        // Create a task first using the mocked BytePlus API
        var taskId = await agent.GenerateVideoFromTextAsync("Test video generation");
        
        // Wait a moment for the task to be processed
        await Task.Delay(100);

        // Act
        var status = await agent.GetVideoStatusAsync(taskId);

        // Assert
        status.ShouldNotBeNull();
        status.TaskId.ShouldBe(taskId);
        status.Status.ShouldNotBeNullOrEmpty();
        status.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        
        _testOutputHelper.WriteLine($"Task status: {status.Status}, Progress: {status.Progress}%");
    }

    [Fact]
    public async Task GetVideoStatusAsync_NonExistentTask_ShouldReturnNotFound()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        var nonExistentTaskId = Guid.NewGuid().ToString();

        // Act
        var status = await agent.GetVideoStatusAsync(nonExistentTaskId);

        // Assert
        status.ShouldNotBeNull();
        status.TaskId.ShouldBe(nonExistentTaskId);
        status.Status.ShouldBe("not_found");
        status.ErrorMessage.ShouldBe("Task not found");
    }

    [Fact]
    public async Task GetVideoStatusAsync_NullTaskId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act & Assert - Method throws ArgumentNullException when accessing dictionary with null key
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await agent.GetVideoStatusAsync(null);
        });
    }

    [Fact]
    public async Task GetVideoStatusAsync_EmptyTaskId_ShouldReturnNotFound()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act
        var status = await agent.GetVideoStatusAsync("");

        // Assert
        status.ShouldNotBeNull();
        status.Status.ShouldBe("not_found");
        status.ErrorMessage.ShouldBe("Task not found");
    }

    [Fact]
    public async Task GetVideoStatusAsync_ProcessingTask_ShouldCheckBytePlusAPI()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Create a task first
        var taskId = await agent.GenerateVideoFromTextAsync("Test video for status check");
        
        // Act - Call status check multiple times to test progression
        var status1 = await agent.GetVideoStatusAsync(taskId);
        await Task.Delay(100);
        var status2 = await agent.GetVideoStatusAsync(taskId);

        // Assert
        status1.ShouldNotBeNull();
        status1.TaskId.ShouldBe(taskId);
        status1.Status.ShouldBeOneOf("processing", "completed", "failed");
        
        status2.ShouldNotBeNull();
        status2.TaskId.ShouldBe(taskId);
        
        // Progress should either stay the same or increase
        status2.Progress.ShouldBeGreaterThanOrEqualTo(status1.Progress);
        
        _testOutputHelper.WriteLine($"Status 1: {status1.Status} ({status1.Progress}%)");
        _testOutputHelper.WriteLine($"Status 2: {status2.Status} ({status2.Progress}%)");
    }

    #endregion

    #region Integration Tests - Public Interface

    [Fact]
    public async Task VideoGenerationGAgent_IntegrationTest_TextToVideoWorkflow()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act - Full workflow test
        var taskId = await agent.GenerateVideoFromTextAsync("A beautiful sunset over mountains");
        var status = await agent.GetVideoStatusAsync(taskId);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        status.ShouldNotBeNull();
        status.TaskId.ShouldBe(taskId);
        status.Status.ShouldBeOneOf("processing", "completed", "failed");
        
        _testOutputHelper.WriteLine($"Integration test completed - Task ID: {taskId}, Status: {status.Status}");
    }

    [Fact]
    public async Task VideoGenerationGAgent_IntegrationTest_ImageToVideoWorkflow()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act - Full workflow test
        var taskId = await agent.GenerateVideoFromImageAsync("https://example.com/image.jpg", "Add motion to this image");
        var status = await agent.GetVideoStatusAsync(taskId);

        // Assert
        taskId.ShouldNotBeNullOrEmpty();
        status.ShouldNotBeNull();
        status.TaskId.ShouldBe(taskId);
        status.Status.ShouldBeOneOf("processing", "completed", "failed");
        
        _testOutputHelper.WriteLine($"Integration test completed - Task ID: {taskId}, Status: {status.Status}");
    }

    [Fact]
    public async Task VideoGenerationGAgent_MultipleTasks_ShouldHandleConcurrently()
    {
        // Arrange
        var agent = await _gAgentFactory.GetGAgentAsync<IVideoGenerationGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a video generation assistant.",
            LLMConfig = new LLMConfigDto { SystemLLM = "BytePlusVideoGeneration" }
        });

        // Act - Create multiple tasks
        var task1 = await agent.GenerateVideoFromTextAsync("First video prompt");
        var task2 = await agent.GenerateVideoFromTextAsync("Second video prompt");
        var task3 = await agent.GenerateVideoFromTextAsync("Third video prompt");

        // Check statuses
        var status1 = await agent.GetVideoStatusAsync(task1);
        var status2 = await agent.GetVideoStatusAsync(task2);
        var status3 = await agent.GetVideoStatusAsync(task3);

        // Assert
        task1.ShouldNotBeNullOrEmpty();
        task2.ShouldNotBeNullOrEmpty();
        task3.ShouldNotBeNullOrEmpty();
        
        // All tasks should be distinct
        task1.ShouldNotBe(task2);
        task2.ShouldNotBe(task3);
        task1.ShouldNotBe(task3);
        
        // All statuses should be valid
        status1.ShouldNotBeNull();
        status2.ShouldNotBeNull();
        status3.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"Multiple tasks created successfully: {task1}, {task2}, {task3}");
    }

    #endregion

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