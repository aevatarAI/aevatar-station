using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Orleans;
using Shouldly;
using System;
using Aevatar.Core.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Orleans.EventSourcing; // Add this import if not present

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Test state for mocking AIGAgentBasePlus
/// </summary>
[GenerateSerializer]
public class TestState : AIGAgentStateBasePlus
{
    [Id(0)] public string TestProperty { get; set; } = "";
}

/// <summary>
/// Test state log event for mocking AIGAgentBasePlus
/// </summary>
[GenerateSerializer] 
public class TestStateLogEvent : StateLogEventBase<TestStateLogEvent>
{
    [Id(0)] public string TestEventData { get; set; } = "";
}

/// <summary>
/// Unit tests for AIGAgentBasePlus public methods following Arrange-Act-Assert pattern
/// Each test actually calls the public methods and verifies their behavior through mocked dependencies
/// 
/// SKIPPED: These unit tests are skipped due to Orleans infrastructure limitations.
/// Orleans Grains require full cluster infrastructure for proper testing.
/// Use AIGAgentBasePlusIntegrationTest instead for testing with full Orleans TestCluster.
/// </summary>
public class AIGAgentBasePlusUnitTest
{
    private readonly Mock<AIGAgentBasePlus<TestState, TestStateLogEvent>> _mockAgent;
    private readonly Mock<IBrainFactory> _mockBrainFactory;
    private readonly Mock<IBrain> _mockBrain;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IOptions<SystemLLMConfigOptions>> _mockSystemLLMOptions;
    private readonly Mock<IOptions<MCPServerConfig>> _mockMCPOptions;

    public AIGAgentBasePlusUnitTest()
    {
        _mockAgent = new Mock<AIGAgentBasePlus<TestState, TestStateLogEvent>>(){ CallBase = true   };
        _mockBrainFactory = new Mock<IBrainFactory>();
        _mockBrain = new Mock<IBrain>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger>();
        _mockSystemLLMOptions = new Mock<IOptions<SystemLLMConfigOptions>>();
        _mockMCPOptions = new Mock<IOptions<MCPServerConfig>>();

        // Setup basic service provider mocks using GetService instead of GetRequiredService (extension method can't be mocked)
        _mockServiceProvider.Setup(x => x.GetService(typeof(IBrainFactory))).Returns(_mockBrainFactory.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<SystemLLMConfigOptions>))).Returns(_mockSystemLLMOptions.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<MCPServerConfig>))).Returns(_mockMCPOptions.Object);

        // Setup system LLM configuration
        _mockSystemLLMOptions.Setup(x => x.Value).Returns(new SystemLLMConfigOptions
        {
            SystemLLMConfigs = new Dictionary<string, LLMConfig>
            {
                ["gpt-4"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.OpenAI,
                    ModelIdEnum = ModelIdEnum.OpenAI,
                    ModelName = "gpt-4"
                }
            }
        });

        _mockMCPOptions.Setup(x => x.Value).Returns(new MCPServerConfig());
    }

    #region InitializeAsync Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task InitializeAsync_WithValidSystemLLM_ShouldReturnTrue()
    {
        // Arrange
        var initDto = new InitializeDto
        {
            LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
            Instructions = "Test instructions"
        };

        _mockBrainFactory.Setup(x => x.CreateBrain(It.IsAny<LLMProviderConfig>()))
            .Returns(_mockBrain.Object);

        // Act
        var result = await _mockAgent.Object.InitializeAsync(initDto);

        // Assert
        result.ShouldBeTrue();
        _mockBrainFactory.Verify(x => x.CreateBrain(It.IsAny<LLMProviderConfig>()), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task InitializeAsync_WithSelfLLMConfig_ShouldReturnTrue()
    {
        // Arrange
        var selfConfig = new SelfLLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelId = ModelIdEnum.OpenAI,
            ModelName = "gpt-3.5-turbo"
        };

        var initDto = new InitializeDto
        {
            LLMConfig = new LLMConfigDto { SelfLLMConfig = selfConfig },
            Instructions = "Custom instructions"
        };

        _mockBrainFactory.Setup(x => x.CreateBrain(It.IsAny<LLMProviderConfig>()))
            .Returns(_mockBrain.Object);

        // Act
        var result = await _mockAgent.Object.InitializeAsync(initDto);

        // Assert
        result.ShouldBeTrue();
        _mockBrainFactory.Verify(x => x.CreateBrain(It.IsAny<LLMProviderConfig>()), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task InitializeAsync_WithNullDto_ShouldReturnFalse()
    {
        // Arrange
        InitializeDto? nullDto = null;

        // Act
        var result = await _mockAgent.Object.InitializeAsync(nullDto);

        // Assert
        result.ShouldBeFalse();
        _mockBrainFactory.Verify(x => x.CreateBrain(It.IsAny<LLMProviderConfig>()), Times.Never);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task InitializeAsync_WhenBrainCreationFails_ShouldThrowException()
    {
        // Arrange
        var initDto = new InitializeDto
        {
            LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
            Instructions = "Test instructions"
        };

        _mockBrainFactory.Setup(x => x.CreateBrain(It.IsAny<LLMProviderConfig>()))
            .Throws(new Exception("Brain creation failed"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => await _mockAgent.Object.InitializeAsync(initDto));
    }

    #endregion

    #region UploadKnowledge Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task UploadKnowledge_WithValidKnowledgeList_ShouldReturnTrue()
    {
        // Arrange
        var knowledgeList = new List<BrainContentDto>
        {
            new BrainContentDto("Test Knowledge", "Test content")
        };

        _mockBrain.Setup(x => x.UpsertKnowledgeAsync(It.IsAny<List<BrainContent>>()))
             .ReturnsAsync(true);

        // Act
        var result = await _mockAgent.Object.UploadKnowledge(knowledgeList);

        // Assert
        result.ShouldBeTrue();
        _mockBrain.Verify(x => x.UpsertKnowledgeAsync(It.IsAny<List<BrainContent>>()), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task UploadKnowledge_WithNullKnowledgeList_ShouldReturnFalse()
    {
        // Arrange
        // Act
        var result = await _mockAgent.Object.UploadKnowledge(null);

        // Assert
        result.ShouldBeFalse();
        _mockBrain.Verify(x => x.UpsertKnowledgeAsync(It.IsAny<List<BrainContent>>()), Times.Never);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task UploadKnowledge_WithEmptyKnowledgeList_ShouldReturnFalse()
    {
        // Arrange
        var knowledgeList = new List<BrainContentDto>();

        // Act
        var result = await _mockAgent.Object.UploadKnowledge(knowledgeList);

        // Assert
        result.ShouldBeFalse();
        _mockBrain.Verify(x => x.UpsertKnowledgeAsync(It.IsAny<List<BrainContent>>()), Times.Never);
    }

    #endregion

    #region GetLLMConfigAsync Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task GetLLMConfigAsync_WhenConfigExists_ShouldReturnConfig()
    {
        // Arrange
        var expectedConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI,
            ModelName = "gpt-4"
        };

        // Act
        var result = await _mockAgent.Object.GetLLMConfigAsync();

        // Assert
        result.ShouldBe(expectedConfig);
        result.ModelName.ShouldBe("gpt-4");
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task GetLLMConfigAsync_WhenConfigDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        // Act
        var result = await _mockAgent.Object.GetLLMConfigAsync();

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SetLLMConfigKeyAsync Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetLLMConfigKeyAsync_WithValidKey_ShouldComplete()
    {
        // Arrange
        var configKey = "test-llm-key";
        
        // Act
        await _mockAgent.Object.SetLLMConfigKeyAsync(configKey);

        // Assert
        _mockAgent.Verify(x => x.SetLLMConfigKeyAsync(configKey), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetLLMConfigKeyAsync_WithEmptyKey_ShouldComplete()
    {
        // Arrange
        var configKey = "";

        // Act
        await _mockAgent.Object.SetLLMConfigKeyAsync(configKey);

        // Assert
        _mockAgent.Verify(x => x.SetLLMConfigKeyAsync(configKey), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetLLMConfigKeyAsync_WithNullKey_ShouldComplete()
    {
        // Arrange
        string configKey = null;
        
        // Act
        await _mockAgent.Object.SetLLMConfigKeyAsync(configKey);

        // Assert
        _mockAgent.Verify(x => x.SetLLMConfigKeyAsync(configKey), Times.Once);
    }

    #endregion

    #region SetSystemLLMAsync Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetSystemLLMAsync_WithValidSystemLLM_ShouldComplete()
    {
        // Arrange
        var systemLLM = "gpt-4";

        // Act
        await _mockAgent.Object.SetSystemLLMAsync(systemLLM);

        // Assert
        _mockAgent.Verify(x => x.SetSystemLLMAsync(systemLLM), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetSystemLLMAsync_WithEmptySystemLLM_ShouldComplete()
    {
        // Arrange
        var systemLLM = "";
        
        // Act
        await _mockAgent.Object.SetSystemLLMAsync(systemLLM);

        // Assert
        _mockAgent.Verify(x => x.SetSystemLLMAsync(systemLLM), Times.Once);
    }

    #endregion

    #region SetLLMAsync Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetLLMAsync_WithValidConfigAndSystemLLM_ShouldComplete()
    {
        // Arrange
        var llmConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI,
            ModelName = "gpt-4"
        };
        var systemLLM = "system-gpt-4";

        // Act
        await _mockAgent.Object.SetLLMAsync(llmConfig, systemLLM);

        // Assert
        _mockAgent.Verify(x => x.SetLLMAsync(llmConfig, systemLLM), Times.Once);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task SetLLMAsync_WithValidConfigAndNullSystemLLM_ShouldComplete()
    {
        // Arrange
        var llmConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAI,
            ModelName = "gpt-35-turbo"
        };

        // Act
        await _mockAgent.Object.SetLLMAsync(llmConfig, null);

        // Assert
        _mockAgent.Verify(x => x.SetLLMAsync(llmConfig, null), Times.Once);
    }

    #endregion

    #region TriggerMigrationAsync Tests

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task TriggerMigrationAsync_ShouldComplete()
    {
        // Arrange
        // Use reflection to access LogViewAdaptor and mock its ConfirmedView (which State returns)
        var testState = new TestState
        {
            SystemLLM = "gpt-4",      // Legacy configuration exists
            LLMConfigKey = null        // No new key - triggers migration
        };
        
        // Create a mock LogViewAdaptor
        var mockLogViewAdaptor = new Mock<Orleans.EventSourcing.ILogViewAdaptor<TestState, object>>();
        mockLogViewAdaptor.SetupGet(x => x.ConfirmedView).Returns(testState);
        mockLogViewAdaptor.Setup(x => x.Submit(It.IsAny<object>()));
        mockLogViewAdaptor.Setup(x => x.ConfirmSubmittedEntries()).Returns(Task.CompletedTask);
        
        // Use reflection to set the internal LogViewAdaptor backing field
        // Since the property has a private setter, we need to find the backing field
        var logViewAdaptorField = _mockAgent.Object.GetType()
            .GetField("<LogViewAdaptor>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (logViewAdaptorField == null)
        {
            // Try alternate backing field naming convention
            logViewAdaptorField = _mockAgent.Object.GetType()
                .GetField("_logViewAdaptor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
        
        logViewAdaptorField?.SetValue(_mockAgent.Object, mockLogViewAdaptor.Object);

        // Act
        await _mockAgent.Object.TriggerMigrationAsync();

        // Assert
        // Verify that Submit was called (RaiseEvent calls LogViewAdaptor.Submit)
        mockLogViewAdaptor.Verify(x => x.Submit(It.IsAny<object>()), Times.Once());
        
        // Verify that ConfirmSubmittedEntries was called (ConfirmEvents calls it)
        mockLogViewAdaptor.Verify(x => x.ConfirmSubmittedEntries(), Times.Once());
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task TriggerMigrationAsync_WhenLegacySystemLLMExists_ShouldRaiseAndConfirmMigrationEvent_Positive()
    {
        // Arrange
        var mockLogViewAdaptor = new Mock<ILogViewAdaptor<TestState, object>>();
        var testState = new TestState { SystemLLM = "legacy-gpt4", LLMConfigKey = null }; // Triggers migration
        SetupLogViewAdaptorForState(mockLogViewAdaptor, testState);
        _mockLogger.Setup(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Debug || l == LogLevel.Information), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        await _mockAgent.Object.TriggerMigrationAsync();

        // Assert
        mockLogViewAdaptor.Verify(x => x.Submit(It.Is<AIGAgentBasePlus<TestState, TestStateLogEvent>.SetLLMConfigKeyStateLogEvent>(e => e.LLMConfigKey == "legacy-gpt4")), Times.Once);
        mockLogViewAdaptor.Verify(x => x.ConfirmSubmittedEntries(), Times.Once);
        testState.LLMConfigKey.ShouldBe("legacy-gpt4"); // Post-state assertion
        _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task TriggerMigrationAsync_WhenNoLegacySystemLLM_ShouldNotRaiseEvent_Negative()
    {
        // Arrange
        var mockLogViewAdaptor = new Mock<ILogViewAdaptor<TestState, object>>();
        var testState = new TestState { SystemLLM = null, LLMConfigKey = "existing-key" }; // No migration needed
        SetupLogViewAdaptorForState(mockLogViewAdaptor, testState);

        // Act
        await _mockAgent.Object.TriggerMigrationAsync();

        // Assert
        mockLogViewAdaptor.Verify(x => x.Submit(It.IsAny<object>()), Times.Never);
        mockLogViewAdaptor.Verify(x => x.ConfirmSubmittedEntries(), Times.Never);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task TriggerMigrationAsync_WithEmptySystemLLM_ShouldNotRaiseEvent_Boundary()
    {
        // Arrange
        var mockLogViewAdaptor = new Mock<ILogViewAdaptor<TestState, object>>();
        var testState = new TestState { SystemLLM = "", LLMConfigKey = null }; // Boundary: empty legacy
        SetupLogViewAdaptorForState(mockLogViewAdaptor, testState);

        // Act
        await _mockAgent.Object.TriggerMigrationAsync();

        // Assert
        mockLogViewAdaptor.Verify(x => x.Submit(It.IsAny<object>()), Times.Never);
    }

    [Fact(Skip = "Orleans infrastructure limitations - use integration tests instead")]
    public async Task TriggerMigrationAsync_WhenConfirmEventsThrows_ShouldHandleException_Exception()
    {
        // Arrange
        var mockLogViewAdaptor = new Mock<ILogViewAdaptor<TestState, object>>();
        var testState = new TestState { SystemLLM = "legacy-gpt4", LLMConfigKey = null };
        mockLogViewAdaptor.Setup(x => x.ConfirmSubmittedEntries()).ThrowsAsync(new InvalidOperationException("Persistence failed"));
        SetupLogViewAdaptorForState(mockLogViewAdaptor, testState);

        // Act
        await _mockAgent.Object.TriggerMigrationAsync(); // Should not throw

        // Assert
        mockLogViewAdaptor.Verify(x => x.Submit(It.IsAny<object>()), Times.Once);
        mockLogViewAdaptor.Verify(x => x.ConfirmSubmittedEntries(), Times.Once);
        _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    private void SetupLogViewAdaptorForState(Mock<ILogViewAdaptor<TestState, object>> mockLogViewAdaptor, TestState state)
    {
        mockLogViewAdaptor.SetupGet(x => x.ConfirmedView).Returns(state);
        mockLogViewAdaptor.Setup(x => x.Submit(It.IsAny<object>()));
        mockLogViewAdaptor.Setup(x => x.ConfirmSubmittedEntries()).Returns(Task.CompletedTask);

        var logViewAdaptorField = _mockAgent.Object.GetType()
            .GetField("<LogViewAdaptor>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? _mockAgent.Object.GetType().GetField("_logViewAdaptor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        logViewAdaptorField?.SetValue(_mockAgent.Object, mockLogViewAdaptor.Object);
    }

}