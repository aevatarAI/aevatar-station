using Aevatar.AI.Agent;
using Aevatar.AI.Brain;
using Aevatar.AI.BrainFactory;
using Aevatar.AI.Dtos;
using Aevatar.AI.State;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Aevatar.AI.AIGAgent.Tests.Agent;

public class TestAIGAgentStateLogEvent : StateLogEventBase<TestAIGAgentStateLogEvent>
{
}

[GenerateSerializer]
public class TestAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> Content { get; set; }
}

public class TestAIGAgent : AIGAgentBase<TestAIGAgentState, TestAIGAgentStateLogEvent>
{
    public TestAIGAgent(ILogger logger) : base(logger)
    {
    }

    public async Task<string?> PublicInvokePromptAsync(string prompt)
    {
        var result = await ChatWithHistory(prompt);
        return result?[0].Content;
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }
}

public class AIGAgentBaseTests : AevatarGAgentsTestBase
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IBrainFactory> _brainFactoryMock;
    private readonly Mock<IBrain> _brainMock;
    private readonly TestAIGAgent _agent;

    public AIGAgentBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        //_brainFactoryMock = new Mock<IBrainFactory>();
        //_brainMock = new Mock<IBrain>();

        //Service.AddSingleton(_brainFactoryMock.Object);

        // Create the agent with the real service provider
        _agent = new TestAIGAgent(_loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_WithValidInput_ShouldReturnTrue()
    {
        // Arrange
        var initializeDto = new InitializeDto
        {
            LLM = "gpt-4",
            Instructions = "Test instructions",
        };

        _brainFactoryMock
            .Setup(x => x.GetBrain(initializeDto.LLM))
            .Returns(_brainMock.Object);

        _brainMock
            .Setup(x => x.InitAsync(
                It.IsAny<string>(),
                initializeDto.Instructions, false));

        // Act
        var result = await _agent.InitializeAsync(initializeDto);

        // Assert
        result.ShouldBeTrue();
        _brainFactoryMock.Verify(x => x.GetBrain(initializeDto.LLM), Times.Once);
        _brainMock.Verify(
            x => x.InitAsync(
                It.IsAny<string>(),
                initializeDto.Instructions, false),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenBrainFactoryReturnsNull_ShouldReturnFalse()
    {
        // Arrange
        var initializeDto = new InitializeDto
        {
            LLM = "invalid-model",
            Instructions = "Test instructions"
        };

        _brainFactoryMock
            .Setup(x => x.GetBrain(initializeDto.LLM))
            .Returns((IBrain?)null);

        // Act
        var result = await _agent.InitializeAsync(initializeDto);

        // Assert
        result.ShouldBeFalse();
        _brainFactoryMock.Verify(x => x.GetBrain(initializeDto.LLM), Times.Once);
    }

    [Fact]
    public async Task InvokePromptAsync_WithInitializedBrain_ShouldReturnResponse()
    {
        // Arrange
        var expectedResponse = "Test response";
        var prompt = "Test prompt";

        // First initialize the brain
        var initializeDto = new InitializeDto
        {
            LLM = "gpt-4",
            Instructions = "Test instructions"
        };

        _brainFactoryMock
            .Setup(x => x.GetBrain(initializeDto.LLM))
            .Returns(_brainMock.Object);

        // _brainMock
        //     .Setup(x => x.InitBrainAsync(
        //         It.IsAny<string>(),
        //         initializeDto.Instructions);

        _brainMock
            .Setup(x => x.ChatAsync(prompt, null))
            .ShouldNotBeNull();

        await _agent.InitializeAsync(initializeDto);

        // Act
        var result = await _agent.PublicInvokePromptAsync(prompt);

        // Assert
        result.ShouldBe(expectedResponse);
        _brainMock.Verify(x => x.ChatAsync(prompt, null), Times.Once);
    }

    [Fact]
    public async Task InvokePromptAsync_WithoutInitializedBrain_ShouldReturnNull()
    {
        // Act
        var result = await _agent.PublicInvokePromptAsync("Test prompt");

        // Assert
        result.ShouldBeNull();
    }
}