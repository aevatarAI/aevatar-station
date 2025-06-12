using Aevatar.Core.Abstractions;
using Aevatar.Core.Plugin;
using Shouldly;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

/// <summary>
/// Unit tests for interface implementation patterns in the Plugin system
/// </summary>
public class InterfaceImplementationTests
{
    #region IMethodCallable Tests

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithValidMethod_ReturnsResult()
    {
        // Arrange
        var callable = new TestMethodCallable();
        var methodName = "TestMethod";
        var parameters = new object[] { "param1", 42 };

        // Act
        var result = await callable.CallMethodAsync(methodName, parameters);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("TestMethod called with param1, 42");
    }

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithNullParameters_HandlesCorrectly()
    {
        // Arrange
        var callable = new TestMethodCallable();
        var methodName = "TestMethodWithNullParams";
        object?[] parameters = null!;

        // Act
        var result = await callable.CallMethodAsync(methodName, parameters);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("TestMethodWithNullParams called with null parameters");
    }

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithEmptyParameters_HandlesCorrectly()
    {
        // Arrange
        var callable = new TestMethodCallable();
        var methodName = "TestMethodWithEmptyParams";
        var parameters = Array.Empty<object>();

        // Act
        var result = await callable.CallMethodAsync(methodName, parameters);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("TestMethodWithEmptyParams called with empty parameters");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NonExistentMethod")]
    public async Task IMethodCallable_CallMethodAsync_WithInvalidMethod_ThrowsException(string methodName)
    {
        // Arrange
        var callable = new TestMethodCallable();
        var parameters = new object[] { "param" };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => callable.CallMethodAsync(methodName, parameters));
    }

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithNullMethodName_ThrowsArgumentNullException()
    {
        // Arrange
        var callable = new TestMethodCallable();
        string methodName = null!;
        var parameters = new object[] { "param" };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => callable.CallMethodAsync(methodName, parameters));
    }

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithComplexParameters_HandlesCorrectly()
    {
        // Arrange
        var callable = new TestMethodCallable();
        var methodName = "TestMethodWithComplexParams";
        var complexParam = new ComplexParameter
        {
            Id = 123,
            Name = "Test",
            Properties = new Dictionary<string, object> { { "key", "value" } }
        };
        var parameters = new object[] { complexParam, DateTime.UtcNow, new[] { 1, 2, 3 } };

        // Act
        var result = await callable.CallMethodAsync(methodName, parameters);

        // Assert
        result.ShouldNotBeNull();
        result.ToString()!.ShouldContain("TestMethodWithComplexParams");
        result.ToString()!.ShouldContain("123");
    }

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithVoidReturnType_ReturnsNull()
    {
        // Arrange
        var callable = new TestMethodCallable();
        var methodName = "VoidMethod";
        var parameters = new object[] { "param" };

        // Act
        var result = await callable.CallMethodAsync(methodName, parameters);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region IEventReceiver Tests

    [Fact]
    public async Task IEventReceiver_ReceiveEventAsync_WithValidEvent_ProcessesCorrectly()
    {
        // Arrange
        var receiver = new TestEventReceiver();
        var eventBase = new TestEventBase
        {
            EventType = "TestEvent",
            Data = "test data",
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(() => receiver.ReceiveEventAsync(eventBase));

        // Verify event was processed
        receiver.LastReceivedEvent.ShouldNotBeNull();
        receiver.LastReceivedEvent.EventType.ShouldBe("TestEvent");
    }

    [Fact]
    public async Task IEventReceiver_ReceiveEventAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var receiver = new TestEventReceiver();
        EventBase eventBase = null!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => receiver.ReceiveEventAsync(eventBase));
    }

    [Fact]
    public async Task IEventReceiver_ReceiveEventAsync_WithCancellationToken_HandlesCancellation()
    {
        // Arrange
        var receiver = new TestEventReceiver();
        var eventBase = new TestEventBase { EventType = "LongRunningEvent" };
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => 
            receiver.ReceiveEventAsync(eventBase, cts.Token));
    }

    [Fact]
    public async Task IEventReceiver_ReceiveEventAsync_WithComplexEventData_HandlesCorrectly()
    {
        // Arrange
        var receiver = new TestEventReceiver();
        var complexData = new ComplexEventData
        {
            Id = 456,
            Properties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "nested", new { SubProperty = "subvalue" } }
            }
        };
        var eventBase = new TestEventBase
        {
            EventType = "ComplexEvent",
            Data = complexData
        };

        // Act
        await receiver.ReceiveEventAsync(eventBase);

        // Assert
        receiver.LastReceivedEvent.ShouldNotBeNull();
        receiver.LastReceivedEvent.EventType.ShouldBe("ComplexEvent");
        receiver.LastReceivedEvent.Data.ShouldBe(complexData);
    }

    [Fact]
    public async Task IEventReceiver_ReceiveEventAsync_WithMultipleEvents_ProcessesAllCorrectly()
    {
        // Arrange
        var receiver = new TestEventReceiver();
        var events = new[]
        {
            new TestEventBase { EventType = "Event1", Data = "data1" },
            new TestEventBase { EventType = "Event2", Data = "data2" },
            new TestEventBase { EventType = "Event3", Data = "data3" }
        };

        // Act
        foreach (var eventBase in events)
        {
            await receiver.ReceiveEventAsync(eventBase);
        }

        // Assert
        receiver.ReceivedEventCount.ShouldBe(3);
        receiver.LastReceivedEvent.EventType.ShouldBe("Event3");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ValidEventType")]
    public async Task IEventReceiver_ReceiveEventAsync_WithVariousEventTypes_HandlesCorrectly(string eventType)
    {
        // Arrange
        var receiver = new TestEventReceiver();
        var eventBase = new TestEventBase { EventType = eventType, Data = "test" };

        // Act & Assert - Should not throw regardless of event type format
        await Should.NotThrowAsync(() => receiver.ReceiveEventAsync(eventBase));
        receiver.LastReceivedEvent.EventType.ShouldBe(eventType);
    }

    #endregion

    #region Interface Inheritance and Implementation Tests

    [Fact]
    public void TestMethodCallable_ImplementsIMethodCallable()
    {
        // Arrange & Act
        var callable = new TestMethodCallable();

        // Assert
        callable.ShouldBeAssignableTo<IMethodCallable>();
    }

    [Fact]
    public void TestEventReceiver_ImplementsIEventReceiver()
    {
        // Arrange & Act
        var receiver = new TestEventReceiver();

        // Assert
        receiver.ShouldBeAssignableTo<IEventReceiver>();
    }

    [Fact]
    public void CombinedImplementation_ImplementsBothInterfaces()
    {
        // Arrange & Act
        var combined = new CombinedImplementation();

        // Assert
        combined.ShouldBeAssignableTo<IMethodCallable>();
        combined.ShouldBeAssignableTo<IEventReceiver>();
    }

    [Fact]
    public async Task CombinedImplementation_BothInterfacesMethods_WorkCorrectly()
    {
        // Arrange
        var combined = new CombinedImplementation();
        var eventBase = new TestEventBase { EventType = "TestEvent", Data = "test" };

        // Act & Assert
        var methodResult = await combined.CallMethodAsync("TestMethod", new object[] { "param" });
        methodResult.ShouldNotBeNull();

        await Should.NotThrowAsync(() => combined.ReceiveEventAsync(eventBase));
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task IMethodCallable_CallMethodAsync_WithException_PropagatesCorrectly()
    {
        // Arrange
        var callable = new TestMethodCallable();
        var methodName = "ThrowingMethod";
        var parameters = new object[] { "param" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            callable.CallMethodAsync(methodName, parameters));
        exception.Message.ShouldBe("Method intentionally throws");
    }

    [Fact]
    public async Task IEventReceiver_ReceiveEventAsync_WithException_PropagatesCorrectly()
    {
        // Arrange
        var receiver = new TestEventReceiver();
        var eventBase = new TestEventBase { EventType = "ErrorEvent", Data = "error" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            receiver.ReceiveEventAsync(eventBase));
        exception.Message.ShouldBe("Error processing event");
    }

    #endregion
}

#region Test Implementation Classes

public class TestMethodCallable : IMethodCallable
{
    public async Task<object?> CallMethodAsync(string methodName, object?[] parameters)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));
            throw new ArgumentException("Method name cannot be empty", nameof(methodName));
        }

        await Task.Delay(1); // Simulate async work

        return methodName switch
        {
            "TestMethod" => $"TestMethod called with {string.Join(", ", parameters ?? Array.Empty<object>())}",
            "TestMethodWithNullParams" => "TestMethodWithNullParams called with null parameters",
            "TestMethodWithEmptyParams" => "TestMethodWithEmptyParams called with empty parameters",
            "TestMethodWithComplexParams" => $"TestMethodWithComplexParams called with {parameters?.Length ?? 0} parameters, first: {parameters?[0]}",
            "VoidMethod" => null,
            "ThrowingMethod" => throw new InvalidOperationException("Method intentionally throws"),
            _ => throw new ArgumentException($"Unknown method: {methodName}")
        };
    }
}

public class TestEventReceiver : IEventReceiver
{
    public EventBase? LastReceivedEvent { get; private set; }
    public int ReceivedEventCount { get; private set; }

    public async Task ReceiveEventAsync(EventBase eventBase, CancellationToken cancellationToken = default)
    {
        if (eventBase == null) throw new ArgumentNullException(nameof(eventBase));

        cancellationToken.ThrowIfCancellationRequested();

        if (eventBase.EventType == "LongRunningEvent")
        {
            await Task.Delay(100, cancellationToken);
        }

        if (eventBase.EventType == "ErrorEvent")
        {
            throw new InvalidOperationException("Error processing event");
        }

        await Task.Delay(1, cancellationToken); // Simulate async work

        LastReceivedEvent = eventBase;
        ReceivedEventCount++;
    }
}

public class CombinedImplementation : IMethodCallable, IEventReceiver
{
    private readonly TestMethodCallable _methodCallable = new();
    private readonly TestEventReceiver _eventReceiver = new();

    public async Task<object?> CallMethodAsync(string methodName, object?[] parameters)
    {
        return await _methodCallable.CallMethodAsync(methodName, parameters);
    }

    public async Task ReceiveEventAsync(EventBase eventBase, CancellationToken cancellationToken = default)
    {
        await _eventReceiver.ReceiveEventAsync(eventBase, cancellationToken);
    }
}

public class TestEventBase : EventBase
{
    public string EventType { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ComplexParameter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class ComplexEventData
{
    public int Id { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

#endregion 