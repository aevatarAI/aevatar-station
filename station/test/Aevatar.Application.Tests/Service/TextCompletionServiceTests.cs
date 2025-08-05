using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Options;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

/// <summary>
/// 文本补全服务单元测试（简化版）
/// </summary>
public class TextCompletionServiceTests
{
    private readonly Mock<IClusterClient> _mockClusterClient;
    private readonly Mock<ILogger<TextCompletionService>> _mockLogger;
    private readonly Mock<IUserAppService> _mockUserAppService;
    private readonly Mock<IGAgentFactory> _mockGAgentFactory;
    private readonly Mock<IOptionsMonitor<AIServicePromptOptions>> _mockPromptOptions;
    private readonly Mock<ITextCompletionGAgent> _mockTextCompletionGAgent;
    private readonly TextCompletionService _service;
    private readonly AIServicePromptOptions _promptOptions;

    public TextCompletionServiceTests()
    {
        _mockClusterClient = new Mock<IClusterClient>();
        _mockLogger = new Mock<ILogger<TextCompletionService>>();
        _mockUserAppService = new Mock<IUserAppService>();
        _mockGAgentFactory = new Mock<IGAgentFactory>();
        _mockPromptOptions = new Mock<IOptionsMonitor<AIServicePromptOptions>>();
        _mockTextCompletionGAgent = new Mock<ITextCompletionGAgent>();

        // Setup default prompt options
        _promptOptions = new AIServicePromptOptions
        {
            TextCompletionSystemRole = "You are a helpful text completion assistant.",
            TextCompletionTaskTemplate = "Complete the following text: {USER_INPUT}",
            TextCompletionImportantRules = "Provide 5 diverse and relevant completions.",
            TextCompletionExamples = "Example: Input: 'The weather today is' -> Output: ['sunny and warm', 'cloudy and cool', 'rainy and wet', 'snowy and cold', 'foggy and humid']",
            TextCompletionOutputRequirements = "Return exactly 5 completion options as a list."
        };

        _mockPromptOptions.Setup(x => x.CurrentValue).Returns(_promptOptions);

        _service = new TextCompletionService(
            _mockClusterClient.Object,
            _mockLogger.Object,
            _mockUserAppService.Object,
            _mockGAgentFactory.Object,
            _mockPromptOptions.Object);
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithValidRequest_ShouldReturnCompletions()
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = "The weather today is very"
        };

        var expectedCompletions = new List<string>
        {
            "sunny and warm",
            "cloudy and cool", 
            "rainy and wet",
            "snowy and cold",
            "foggy and humid"
        };

        SetupMockDependencies(expectedCompletions);

        // Act
        var result = await _service.GenerateCompletionsAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Completions.ShouldNotBeNull();
        result.Completions.Count.ShouldBe(5);
        result.Completions.ShouldBe(expectedCompletions);
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithEmptyCompletions_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = "Simple text to complete"
        };

        var emptyCompletions = new List<string>();
        SetupMockDependencies(emptyCompletions);

        // Act
        var result = await _service.GenerateCompletionsAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Completions.ShouldNotBeNull();
        result.Completions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Short goal")]
    [InlineData("Medium length goal for testing")]
    [InlineData("This is a very long user goal that exceeds fifty characters and should be previewed differently in logs")]
    public async Task GenerateCompletionsAsync_WithDifferentGoalLengths_ShouldHandleCorrectly(string userGoal)
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = userGoal
        };

        var completions = new List<string> { "completion1", "completion2", "completion3", "completion4", "completion5" };
        SetupMockDependencies(completions);

        // Act
        var result = await _service.GenerateCompletionsAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Completions.Count.ShouldBe(5);
    }

    #region Helper Methods

    private void SetupMockDependencies(List<string> completions)
    {
        _mockGAgentFactory.Setup(x => x.GetGAgentAsync<ITextCompletionGAgent>(It.IsAny<Guid>(), null))
            .ReturnsAsync(_mockTextCompletionGAgent.Object);

        _mockTextCompletionGAgent.Setup(x => x.GenerateCompletionsAsync(string.Empty))
            .ReturnsAsync(completions);
    }

    #endregion
} 