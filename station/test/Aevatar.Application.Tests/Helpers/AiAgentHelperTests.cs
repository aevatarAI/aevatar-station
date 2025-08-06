using Aevatar.Application.Grains.Agents.AI;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.Application.Tests.Helpers;

public class AiAgentHelperTests
{
    private readonly ITestOutputHelper _output;

    public AiAgentHelperTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(null, "", "null input")]
    [InlineData("", "", "empty input")]
    [InlineData("   ", "", "whitespace input")]
    [InlineData("test", "fallback", "normal with fallback")]
    public void CleanJsonContent_WithFallback_ShouldHandleNullAndEmpty(string input, string fallback, string description)
    {
        // Act
        var result = AiAgentHelper.CleanJsonContent(input, fallback);
        
        // Assert
        if (string.IsNullOrWhiteSpace(input))
        {
            result.ShouldBe(fallback);
        }
        else
        {
            result.ShouldBe(input.Trim());
        }
        
        _output.WriteLine($"CleanJsonContent test passed for: {description}");
    }

    [Theory]
    [InlineData("```json\n{\"test\": \"value\"}\n```", "{\"test\": \"value\"}")]
    [InlineData("```\n{\"test\": \"value\"}\n```", "{\"test\": \"value\"}")]
    [InlineData("```json\n{\"test\": \"value\"}", "{\"test\": \"value\"}")]
    [InlineData("{\"test\": \"value\"}\n```", "{\"test\": \"value\"}")]
    [InlineData("   ```json\n{\"test\": \"value\"}\n```   ", "{\"test\": \"value\"}")]
    public void CleanJsonContent_MarkdownRemoval_ShouldCleanCorrectly(string input, string expected)
    {
        // Act
        var result = AiAgentHelper.CleanJsonContent(input);
        
        // Assert
        result.ShouldBe(expected);
        
        _output.WriteLine($"Markdown cleaning test passed: '{input}' -> '{result}'");
    }

    [Theory]
    [InlineData("```JSON\n{\"test\": \"value\"}\n```", "{\"test\": \"value\"}")]
    [InlineData("```Json\n{\"test\": \"value\"}\n```", "{\"test\": \"value\"}")]
    [InlineData("```jSoN\n{\"test\": \"value\"}\n```", "{\"test\": \"value\"}")]
    public void CleanJsonContent_CaseInsensitive_ShouldWork(string input, string expected)
    {
        // Act
        var result = AiAgentHelper.CleanJsonContent(input);
        
        // Assert
        result.ShouldBe(expected);
        
        _output.WriteLine($"Case insensitive test passed: '{input}' -> '{result}'");
    }

    [Theory]
    [InlineData("{\"valid\": \"json\"}", true)]
    [InlineData("```json\n{\"valid\": \"json\"}\n```", true)]
    [InlineData("invalid json", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("{\"unclosed\": ", false)]
    public void IsValidJson_Various_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = AiAgentHelper.IsValidJson(input);
        
        // Assert
        result.ShouldBe(expected);
        
        _output.WriteLine($"JSON validation test: '{input}' -> {result}");
    }

    [Theory]
    [InlineData("{\"test\": \"value\"}", "test", "value")]
    [InlineData("```json\n{\"test\": \"value\"}\n```", "test", "value")]
    [InlineData("invalid json", "test", null)]
    [InlineData(null, "test", null)]
    public void SafeParseJson_Various_ShouldParseCorrectly(string input, string propertyName, string expectedValue)
    {
        // Act
        var result = AiAgentHelper.SafeParseJson(input);
        
        // Assert
        if (expectedValue != null)
        {
            result.ShouldNotBeNull();
            result[propertyName]?.ToString().ShouldBe(expectedValue);
        }
        else
        {
            result.ShouldBeNull();
        }
        
        _output.WriteLine($"Safe JSON parsing test: '{input}' -> {(result != null ? "parsed" : "null")}");
    }

    [Fact]
    public void SafeGetStringArray_ValidArray_ShouldReturnCorrectArray()
    {
        // Arrange
        var json = AiAgentHelper.SafeParseJson("{\"items\": [\"a\", \"b\", \"c\"]}");
        
        // Act
        var result = AiAgentHelper.SafeGetStringArray(json, "items", 5);
        
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(5);
        result[0].ShouldBe("a");
        result[1].ShouldBe("b");
        result[2].ShouldBe("c");
        result[3].ShouldBe("");
        result[4].ShouldBe("");
        
        _output.WriteLine("SafeGetStringArray with padding test passed");
    }

    [Fact]
    public void SafeGetStringArray_LargeArray_ShouldTruncateToSize()
    {
        // Arrange
        var json = AiAgentHelper.SafeParseJson("{\"items\": [\"a\", \"b\", \"c\", \"d\", \"e\", \"f\", \"g\"]}");
        
        // Act
        var result = AiAgentHelper.SafeGetStringArray(json, "items", 3);
        
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(3);
        result[0].ShouldBe("a");
        result[1].ShouldBe("b");
        result[2].ShouldBe("c");
        
        _output.WriteLine("SafeGetStringArray truncation test passed");
    }

    [Fact]
    public void SafeGetStringArray_MissingProperty_ShouldReturnDefaultArray()
    {
        // Arrange
        var json = AiAgentHelper.SafeParseJson("{\"other\": \"value\"}");
        
        // Act
        var result = AiAgentHelper.SafeGetStringArray(json, "missing", 3);
        
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(3);
        result.ShouldAllBe(item => item == "");
        
        _output.WriteLine("SafeGetStringArray missing property test passed");
    }

    [Fact]
    public void SafeGetStringArray_NullJson_ShouldReturnDefaultArray()
    {
        // Act
        var result = AiAgentHelper.SafeGetStringArray(null, "items", 2);
        
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldAllBe(item => item == "");
        
        _output.WriteLine("SafeGetStringArray null JSON test passed");
    }

    [Theory]
    [InlineData(null, "Please provide assistance as instructed.")]
    [InlineData("", "Please provide assistance as instructed.")]
    [InlineData("   ", "Please provide assistance as instructed.")]
    [InlineData("test input", "test input")]
    [InlineData("  test input  ", "test input")]
    public void NormalizeUserInput_Various_ShouldNormalizeCorrectly(string input, string expected)
    {
        // Act
        var result = AiAgentHelper.NormalizeUserInput(input);
        
        // Assert
        result.ShouldBe(expected);
        
        _output.WriteLine($"NormalizeUserInput test: '{input}' -> '{result}'");
    }

    [Fact]
    public void NormalizeUserInput_WithCustomDefault_ShouldUseCustomDefault()
    {
        // Act
        var result = AiAgentHelper.NormalizeUserInput("", "Custom default message");
        
        // Assert
        result.ShouldBe("Custom default message");
        
        _output.WriteLine("Custom default message test passed");
    }
}