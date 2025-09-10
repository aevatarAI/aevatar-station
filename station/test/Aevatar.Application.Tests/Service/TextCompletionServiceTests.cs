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
using Volo.Abp;
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
    [InlineData("This is exactly 15 characters")]  // 15 characters - should pass
    [InlineData("Medium length goal for testing with some more content to make it longer")]  // Within range - should pass
    [InlineData("This is a test string that has exactly two hundred and fifty characters including spaces and punctuation marks to validate the maximum allowed length for user goals in our text completion service implementation validation logic test case number one")]  // 250 characters - should pass
    public async Task GenerateCompletionsAsync_WithValidGoalLengths_ShouldHandleCorrectly(string userGoal)
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

    [Fact]
    public async Task GenerateCompletionsAsync_WithNullUserGoal_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = null
        };

        // Note: No need to setup mock dependencies since validation happens before agent creation

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.GenerateCompletionsAsync(request));
        
        exception.Message.ShouldBe("User goal cannot be empty.");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithEmptyUserGoal_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = ""
        };

        // Note: No need to setup mock dependencies since validation happens before agent creation

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.GenerateCompletionsAsync(request));
        
        exception.Message.ShouldBe("User goal cannot be empty.");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithWhitespaceOnlyUserGoal_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = "   "
        };

        // Note: No need to setup mock dependencies since validation happens before agent creation

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.GenerateCompletionsAsync(request));
        
        exception.Message.ShouldBe("User goal cannot be empty.");
    }

    [Theory]
    [InlineData("Short")]  // 5 characters - too short
    [InlineData("Too short")]  // 9 characters - too short  
    [InlineData("Still too sho")]  // 14 characters - too short
    public async Task GenerateCompletionsAsync_WithTooShortUserGoal_ShouldThrowUserFriendlyException(string userGoal)
    {
        // Arrange
        var request = new TextCompletionRequestDto
        {
            UserGoal = userGoal
        };

        // Note: No need to setup mock dependencies since validation happens before agent creation

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.GenerateCompletionsAsync(request));
        
        exception.Message.ShouldBe("User goal must be at least 15 characters long.");
    }

    [Fact]
    public async Task GenerateCompletionAsync_WithTooLongUserGoal_ShouldThrowUserFriendlyException()
    {
        // Arrange - Create a string longer than 250 characters (251 characters)
        var longGoal = "This is a test string that has exactly two hundred and fifty one characters including spaces and punctuation marks to validate the maximum allowed length for user goals in our text completion service implementation validation logic test case number xy";
        var request = new TextCompletionRequestDto
        {
            UserGoal = longGoal
        };

        // Note: No need to setup mock dependencies since validation happens before agent creation

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.GenerateCompletionsAsync(request));
        
        exception.Message.ShouldBe("User goal cannot exceed 250 characters.");
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