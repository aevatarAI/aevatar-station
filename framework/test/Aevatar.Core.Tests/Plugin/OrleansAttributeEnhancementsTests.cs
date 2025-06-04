using System.Reflection;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Concurrency;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

public class OrleansAttributeEnhancementsTests
{
    private readonly Mock<ILogger> _loggerMock;

    public OrleansAttributeEnhancementsTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void AttributeCompatibilityValidator_MatchingAttributes_ReturnsCompatible()
    {
        // Arrange
        var pluginAttr = new AgentMethodAttribute { IsReadOnly = true };
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.ReadOnlyMethod))!;

        // Act
        var result = OrleansAttributeEnhancements.AttributeCompatibilityValidator
            .ValidateCompatibility(pluginAttr, method, _loggerMock.Object);

        // Assert
        Assert.True(result.IsCompatible);
        Assert.Empty(result.Issues);
        Assert.Equal(nameof(ITestGrainInterface.ReadOnlyMethod), result.MethodName);
    }

    [Fact]
    public void AttributeCompatibilityValidator_MismatchedAttributes_ReturnsIncompatible()
    {
        // Arrange
        var pluginAttr = new AgentMethodAttribute { IsReadOnly = false }; // Plugin says not read-only
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.ReadOnlyMethod))!; // Interface says read-only

        // Act
        var result = OrleansAttributeEnhancements.AttributeCompatibilityValidator
            .ValidateCompatibility(pluginAttr, method, _loggerMock.Object);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal("ReadOnly", result.Issues[0].AttributeType);
        Assert.False(result.Issues[0].PluginValue);
        Assert.True(result.Issues[0].InterfaceValue);
        Assert.Contains("Add IsReadOnly = true to plugin method", result.Issues[0].Recommendation);
    }

    [Fact]
    public void AttributeCompatibilityValidator_OneWayMismatch_ReturnsError()
    {
        // Arrange
        var pluginAttr = new AgentMethodAttribute { OneWay = false }; // Plugin says not one-way
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.OneWayMethod))!; // Interface says one-way

        // Act
        var result = OrleansAttributeEnhancements.AttributeCompatibilityValidator
            .ValidateCompatibility(pluginAttr, method, _loggerMock.Object);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal(AttributeIssueSeverity.Error, result.Issues[0].Severity);
        Assert.Equal("OneWay", result.Issues[0].AttributeType);
    }

    [Fact]
    public void AttributePerformanceAnalyzer_HighOptimizationMethod_ReturnsHighLevel()
    {
        // Arrange
        var routingInfo = new MethodRoutingInfo
        {
            MethodName = "GetDataAsync",
            IsReadOnly = true,
            AlwaysInterleave = true,
            OneWay = true
        };

        // Act
        var metrics = OrleansAttributeEnhancements.AttributePerformanceAnalyzer
            .AnalyzePerformanceImpact(routingInfo);

        // Assert
        Assert.Equal(OptimizationLevel.High, metrics.EstimatedOptimizationLevel);
        Assert.Equal(ConcurrencyLevel.Maximum, metrics.ConcurrencyPotential);
        Assert.Equal("GetDataAsync", metrics.MethodName);
    }

    [Fact]
    public void AttributePerformanceAnalyzer_NoOptimization_ReturnsSequential()
    {
        // Arrange
        var routingInfo = new MethodRoutingInfo
        {
            MethodName = "UpdateDataAsync",
            IsReadOnly = false,
            AlwaysInterleave = false,
            OneWay = false
        };

        // Act
        var metrics = OrleansAttributeEnhancements.AttributePerformanceAnalyzer
            .AnalyzePerformanceImpact(routingInfo);

        // Assert
        Assert.Equal(OptimizationLevel.None, metrics.EstimatedOptimizationLevel);
        Assert.Equal(ConcurrencyLevel.Sequential, metrics.ConcurrencyPotential);
    }

    [Fact]
    public void AttributeSuggestionEngine_ReadOnlyPattern_SuggestsReadOnly()
    {
        // Arrange
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.GetDataAsync))!;

        // Act
        var suggestion = OrleansAttributeEnhancements.AttributeSuggestionEngine.SuggestAttributes(method);

        // Assert
        Assert.Equal("GetDataAsync", suggestion.MethodName);
        Assert.Contains(suggestion.SuggestedAttributes, attr => attr.AttributeType == typeof(ReadOnlyAttribute));
        
        var readOnlySuggestion = suggestion.SuggestedAttributes.First(attr => attr.AttributeType == typeof(ReadOnlyAttribute));
        Assert.True(readOnlySuggestion.Confidence > 0.7);
        Assert.Contains("read-only", readOnlySuggestion.Reason);
    }

    [Fact]
    public void AttributeSuggestionEngine_LogMethod_SuggestsOneWay()
    {
        // Arrange
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.LogEventAsync))!;

        // Act
        var suggestion = OrleansAttributeEnhancements.AttributeSuggestionEngine.SuggestAttributes(method);

        // Assert
        Assert.Contains(suggestion.SuggestedAttributes, attr => attr.AttributeType == typeof(OneWayAttribute));
        
        var oneWaySuggestion = suggestion.SuggestedAttributes.First(attr => attr.AttributeType == typeof(OneWayAttribute));
        Assert.True(oneWaySuggestion.Confidence > 0.6);
        Assert.Contains("fire-and-forget", oneWaySuggestion.Reason);
    }

    [Fact]
    public void AttributeSuggestionEngine_AsyncNonModifying_SuggestsInterleave()
    {
        // Arrange
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.ProcessDataAsync))!;

        // Act
        var suggestion = OrleansAttributeEnhancements.AttributeSuggestionEngine.SuggestAttributes(method);

        // Assert
        Assert.Contains(suggestion.SuggestedAttributes, attr => attr.AttributeType == typeof(AlwaysInterleaveAttribute));
        
        var interleaveSuggestion = suggestion.SuggestedAttributes.First(attr => attr.AttributeType == typeof(AlwaysInterleaveAttribute));
        Assert.True(interleaveSuggestion.Confidence > 0.5);
        Assert.Contains("concurrent execution", interleaveSuggestion.Reason);
    }

    [Fact]
    public void AttributePerformanceAnalyzer_GeneratesRecommendations()
    {
        // Arrange
        var routingInfo = new MethodRoutingInfo
        {
            MethodName = "GetUserDataAsync", // Should suggest ReadOnly
            IsReadOnly = false,
            AlwaysInterleave = false,
            OneWay = false
        };

        // Act
        var metrics = OrleansAttributeEnhancements.AttributePerformanceAnalyzer
            .AnalyzePerformanceImpact(routingInfo);

        // Assert
        Assert.Contains(metrics.PerformanceRecommendations, 
            rec => rec.Contains("[ReadOnly]") && rec.Contains("doesn't modify grain state"));
    }

    [Fact]
    public void AttributeCompatibilityValidator_LogsWarningsForIncompatibility()
    {
        // Arrange
        var pluginAttr = new AgentMethodAttribute 
        { 
            IsReadOnly = false, 
            AlwaysInterleave = true, 
            OneWay = false 
        };
        var method = typeof(ITestGrainInterface).GetMethod(nameof(ITestGrainInterface.ReadOnlyMethod))!;

        // Act
        var result = OrleansAttributeEnhancements.AttributeCompatibilityValidator
            .ValidateCompatibility(pluginAttr, method, _loggerMock.Object);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Equal(2, result.Issues.Count); // ReadOnly mismatch + AlwaysInterleave mismatch
        
        // Verify logging was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Orleans attribute compatibility issues")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

/// <summary>
/// Test grain interface for attribute enhancement testing
/// </summary>
public interface ITestGrainInterface
{
    [ReadOnly]
    Task<string> ReadOnlyMethod();

    [OneWay]
    Task OneWayMethod();

    Task<string> GetDataAsync();
    
    Task LogEventAsync();
    
    Task ProcessDataAsync();
} 