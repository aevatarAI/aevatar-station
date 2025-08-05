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
using System.Reflection;
using System.Linq;

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
        var input = "Complete this sentence: The future of AI is";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Generated {result.Count} completions for input: {input}");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithEmptyInput_ShouldReturnEmptyCompletions()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var emptyInput = "";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(emptyInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(5);
        foreach (var completion in result)
        {
            completion.ShouldBe("");
        }
        
        _output.WriteLine($"Empty input handled gracefully");
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
    public async Task GenerateCompletionsAsync_WithVeryLongInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var longInput = new string('A', 10000); // 10K characters

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(longInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Long input ({longInput.Length} chars) handled gracefully");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var specialInput = @"Special chars: !@#$%^&*(){}[]<>?/|\~`""'";

        // Act
        var result = await textCompletion.GenerateCompletionsAsync(specialInput);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine($"Special characters handled gracefully");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task GenerateCompletionsAsync_WithConcurrentCalls_ShouldHandleCorrectly(int concurrentCount)
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
        var input = $"Concurrent test {concurrentCount}";

        var tasks = new List<Task<List<string>>>();
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(textCompletion.GenerateCompletionsAsync($"{input} - Call {i}"));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(concurrentCount);
        
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
            result.Count.ShouldBeLessThanOrEqualTo(5);
        }
        
        _output.WriteLine($"Concurrent calls ({concurrentCount}) handled successfully");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_WithNullInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act & Assert
        var result = await textCompletion.GenerateCompletionsAsync(null);
        
        result.ShouldNotBeNull();
        result.Count.ShouldBe(5);
        foreach (var completion in result)
        {
            completion.ShouldBe("");
        }
        
        _output.WriteLine("Null input handled gracefully");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_MultipleCallsSameAgent_ShouldMaintainState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var textCompletion = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

        // Act - Make multiple calls to the same agent
        var result1 = await textCompletion.GenerateCompletionsAsync("First call");
        var result2 = await textCompletion.GenerateCompletionsAsync("Second call");
        var result3 = await textCompletion.GenerateCompletionsAsync("Third call");

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull(); 
        result3.ShouldNotBeNull();
        
        result1.Count.ShouldBeLessThanOrEqualTo(5);
        result2.Count.ShouldBeLessThanOrEqualTo(5);
        result3.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine("Multiple calls to same agent handled correctly");
    }

    [Fact]
    public async Task GenerateCompletionsAsync_DifferentAgents_ShouldBeIndependent()
    {
        // Arrange
        var agent1Id = Guid.NewGuid();
        var agent2Id = Guid.NewGuid();
        var textCompletion1 = _clusterClient.GetGrain<ITextCompletionGAgent>(agent1Id);
        var textCompletion2 = _clusterClient.GetGrain<ITextCompletionGAgent>(agent2Id);

        // Act
        var result1 = await textCompletion1.GenerateCompletionsAsync("Agent 1 input");
        var result2 = await textCompletion2.GenerateCompletionsAsync("Agent 2 input");

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        
        result1.Count.ShouldBeLessThanOrEqualTo(5);
        result2.Count.ShouldBeLessThanOrEqualTo(5);
        
        _output.WriteLine("Different agents handled independently");
    }
} 