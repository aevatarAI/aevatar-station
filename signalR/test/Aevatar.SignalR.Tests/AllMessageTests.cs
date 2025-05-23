using Microsoft.AspNetCore.SignalR.Protocol;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class AllMessageTests
{
    [Fact]
    public void Constructor_WithMessageOnly_ShouldInitializeCorrectly()
    {
        // Arrange
        var invocationMessage = new InvocationMessage("testMethod", new object[] { "arg1", 42 });
        
        // Act
        var allMessage = new AllMessage(invocationMessage);
        
        // Assert
        allMessage.Message.ShouldBeSameAs(invocationMessage);
        allMessage.ExcludedIds.ShouldBeNull();
    }
    
    [Fact]
    public void Constructor_WithMessageAndExcludedIds_ShouldInitializeCorrectly()
    {
        // Arrange
        var invocationMessage = new InvocationMessage("testMethod", new object[] { "arg1", 42 });
        var excludedIds = new List<string> { "connection1", "connection2" };
        
        // Act
        var allMessage = new AllMessage(invocationMessage, excludedIds);
        
        // Assert
        allMessage.Message.ShouldBeSameAs(invocationMessage);
        allMessage.ExcludedIds.ShouldBeSameAs(excludedIds);
    }
    
    [Fact]
    public void EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var message1 = new InvocationMessage("method1", new object[] { "arg1" });
        var excludedIds1 = new List<string> { "conn1", "conn2" };
        
        var allMessage1 = new AllMessage(message1, excludedIds1);
        var allMessage2 = new AllMessage(message1, excludedIds1);
        var allMessage3 = new AllMessage(message1, null);
        
        // Act & Assert - Same values should be equal
        allMessage1.Equals(allMessage2).ShouldBeTrue();
        (allMessage1 == allMessage2).ShouldBeTrue();
        
        // Act & Assert - Different values should not be equal
        allMessage1.Equals(allMessage3).ShouldBeFalse();
        (allMessage1 == allMessage3).ShouldBeFalse();
    }
    
    [Fact]
    public void WithMethods_ShouldCreateNewInstanceWithModifiedProperty()
    {
        // Arrange
        var message1 = new InvocationMessage("method1", new object[] { "arg1" });
        var message2 = new InvocationMessage("method2", new object[] { "arg2" });
        var excludedIds1 = new List<string> { "conn1", "conn2" };
        var excludedIds2 = new List<string> { "conn3", "conn4" };
        
        var original = new AllMessage(message1, excludedIds1);
        
        // Act - Create new instances with modified properties
        var withNewMessage = original with { Message = message2 };
        var withNewExcludedIds = original with { ExcludedIds = excludedIds2 };
        
        // Assert - New instances have modified properties
        withNewMessage.Message.ShouldBe(message2);
        withNewMessage.ExcludedIds.ShouldBe(excludedIds1);
        
        withNewExcludedIds.Message.ShouldBe(message1);
        withNewExcludedIds.ExcludedIds.ShouldBe(excludedIds2);
        
        // Assert - Original remains unchanged
        original.Message.ShouldBe(message1);
        original.ExcludedIds.ShouldBe(excludedIds1);
    }
} 