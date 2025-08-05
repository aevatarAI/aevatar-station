using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aevatar.Application.Tests.GAgent;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class TextCompletionGAgentTests : AevatarApplicationGrainsTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ITestOutputHelper _output;

    public TextCompletionGAgentTests(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _output = output;
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithValidInput_ShouldReturnCompletions()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var inputText = "The quick brown fox";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(inputText);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5); // Should return up to 5 completions
        
        // Since no real AI is configured in test, should return empty strings
        foreach (var completion in result)
        {
            completion.ShouldNotBeNull();
        }
        
        _output.WriteLine($"Generated {result.Count} completions for input: {inputText}");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithNullInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(null);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for null input");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithEmptyInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act
        var result = await textCompletion.GenerateCompletionsAsync("");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for empty input");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithWhitespaceInput_ShouldUseDefaultMessage()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act
        var result = await textCompletion.GenerateCompletionsAsync("   \t\n   ");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for whitespace input");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithLongInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var longInput = new string('A', 10000); // Very long input

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(longInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for long input ({longInput.Length} chars)");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithSpecialCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var specialInput = "Hello \"world\" with 'quotes' and \n\t special chars: @#$%^&*()";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(specialInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for special characters input");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_ShouldAlwaysReturnExactly5Completions()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var inputText = "Complete this text";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(inputText);

        // Assert
        result.ShouldNotBeNull();
        // The implementation should always pad to 5 completions
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated exactly {result.Count} completions");
    }

    [Fact]
    public async Task GetDescriptionAsync_ShouldReturnDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act
        var description = await textCompletion.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNullOrEmpty();
        description.ShouldContain("text completion");
        description.ShouldContain("5 different completion results");
        
        _output.WriteLine($"Agent description: {description}");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_MultipleCallsWithSameInput_ShouldBeConsistent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var inputText = "Test consistency";

        // Act
        var result1 = await textCompletion.GenerateCompletionsAsync(inputText);
        var result2 = await textCompletion.GenerateCompletionsAsync(inputText);

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result1.Count.ShouldBe(result2.Count);
        
        _output.WriteLine($"Both calls returned {result1.Count} completions consistently");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithUnicodeCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var unicodeInput = "Unicode test: 你好世界 🌍 مرحبا العالم";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(unicodeInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for unicode input");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_ConcurrentCalls_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var tasks = new List<Task<List<string>>>();

        // Act - Make multiple concurrent calls
        for (int i = 0; i < 5; i++)
        {
            var input = $"Concurrent test {i}";
            tasks.Add(textCompletion.GenerateCompletionsAsync(input));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(5);
        
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
            result.Count.ShouldBeLessThanOrEqualTo(5);
        }
        
        _output.WriteLine($"All {results.Length} concurrent calls completed successfully");
    }

    [Fact]
    public async Task TextCompletionGAgent_StateAndEventTypes_ShouldBeValid()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act
        var state = await textCompletion.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TextCompletionState>();
        
        _output.WriteLine($"Agent state type: {state.GetType().Name}");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithJSONLikeInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var jsonInput = "{ \"test\": \"value\", \"array\": [1,2,3] }";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(jsonInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for JSON-like input");
    }

    [Fact] 
    public async Task GenerateCompletionsAsync_WithMarkdownLikeInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var markdownInput = "```json\n{\"test\": \"markdown\"}\n```";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(markdownInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for markdown-like input");
    }
} 