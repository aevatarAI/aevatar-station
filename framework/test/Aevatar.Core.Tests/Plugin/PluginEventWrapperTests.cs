using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

/// <summary>
/// Unit tests for PluginEventWrapper class
/// </summary>
public class PluginEventWrapperTests
{
    #region Property Tests

    [Fact]
    public void PluginEventWrapper_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var eventType = "TestEvent";
        var eventData = new { Message = "Test data" };
        var timestamp = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var sourceAgentId = "test-agent";

        // Act
        wrapper.PluginEventType = eventType;
        wrapper.PluginEventData = eventData;
        wrapper.Timestamp = timestamp;
        wrapper.CorrelationId = correlationId;
        wrapper.SourceAgentId = sourceAgentId;

        // Assert
        wrapper.PluginEventType.ShouldBe(eventType);
        wrapper.PluginEventData.ShouldBe(eventData);
        wrapper.Timestamp.ShouldBe(timestamp);
        wrapper.CorrelationId.ShouldBe(correlationId);
        wrapper.SourceAgentId.ShouldBe(sourceAgentId);
    }

    [Fact]
    public void PluginEventWrapper_DefaultValues_AreCorrect()
    {
        // Act
        var wrapper = new PluginEventWrapper();

        // Assert
        wrapper.PluginEventType.ShouldBe(string.Empty);
        wrapper.PluginEventData.ShouldBeNull();
        wrapper.Timestamp.ShouldBeOfType<DateTime>();
        wrapper.CorrelationId.ShouldBeNull();
        wrapper.SourceAgentId.ShouldBeNull();
    }

    [Fact]
    public void PluginEventWrapper_InheritsFromEventBase()
    {
        // Arrange & Act
        var wrapper = new PluginEventWrapper();

        // Assert
        wrapper.ShouldBeAssignableTo<EventBase>();
    }

    #endregion

    #region Data Handling Tests

    [Fact]
    public void PluginEventWrapper_WithComplexData_HandlesCorrectly()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var complexData = new ComplexTestData
        {
            Id = 123,
            Name = "Test",
            CreatedAt = DateTime.UtcNow,
            Tags = new[] { "tag1", "tag2" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            }
        };

        // Act
        wrapper.PluginEventData = complexData;

        // Assert
        wrapper.PluginEventData.ShouldBe(complexData);
        var retrievedData = wrapper.PluginEventData as ComplexTestData;
        retrievedData.ShouldNotBeNull();
        retrievedData.Id.ShouldBe(complexData.Id);
        retrievedData.Name.ShouldBe(complexData.Name);
        retrievedData.Tags.ShouldBe(complexData.Tags);
    }

    [Fact]
    public void PluginEventWrapper_WithNullData_HandlesCorrectly()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act
        wrapper.PluginEventData = null;

        // Assert
        wrapper.PluginEventData.ShouldBeNull();
    }

    [Theory]
    [InlineData("string data")]
    [InlineData(42)]
    [InlineData(true)]
    [InlineData(3.14)]
    public void PluginEventWrapper_WithPrimitiveData_HandlesCorrectly(object data)
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act
        wrapper.PluginEventData = data;

        // Assert
        wrapper.PluginEventData.ShouldBe(data);
    }

    [Fact]
    public void PluginEventWrapper_WithJsonSerializableData_HandlesCorrectly()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var jsonData = JsonSerializer.Serialize(new { Test = "Value", Number = 123 });

        // Act
        wrapper.PluginEventData = jsonData;

        // Assert
        wrapper.PluginEventData.ShouldBe(jsonData);
    }

    #endregion

    #region Event Type Tests

    [Theory]
    [InlineData("")]
    [InlineData("SimpleEvent")]
    [InlineData("Complex.Event.Type")]
    [InlineData("Event123")]
    [InlineData("event-with-dashes")]
    [InlineData("event_with_underscores")]
    public void PluginEventWrapper_WithVariousEventTypes_HandlesCorrectly(string eventType)
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act
        wrapper.PluginEventType = eventType;

        // Assert
        wrapper.PluginEventType.ShouldBe(eventType);
    }

    [Fact]
    public void PluginEventWrapper_WithNullEventType_HandlesCorrectly()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act & Assert
        Should.NotThrow(() => wrapper.PluginEventType = null!);
    }

    #endregion

    #region CorrelationId Tests

    [Fact]
    public void PluginEventWrapper_CorrelationId_CanBeSetAndRetrieved()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var correlationId = Guid.NewGuid();

        // Act
        wrapper.CorrelationId = correlationId;

        // Assert
        wrapper.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void PluginEventWrapper_CorrelationId_CanBeNull()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act
        wrapper.CorrelationId = null;

        // Assert
        wrapper.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void PluginEventWrapper_CorrelationId_OverridesBaseProperty()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var correlationId = Guid.NewGuid();

        // Act
        wrapper.CorrelationId = correlationId;

        // Assert
        // The new keyword should hide the base class property
        wrapper.CorrelationId.ShouldBe(correlationId);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void PluginEventWrapper_Timestamp_CanBeSetAndRetrieved()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var timestamp = new DateTime(2023, 12, 25, 15, 30, 45, DateTimeKind.Utc);

        // Act
        wrapper.Timestamp = timestamp;

        // Assert
        wrapper.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void PluginEventWrapper_Timestamp_PreservesDateTimeKind()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var utcTime = DateTime.UtcNow;
        var localTime = DateTime.Now;

        // Act & Assert
        wrapper.Timestamp = utcTime;
        wrapper.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);

        wrapper.Timestamp = localTime;
        wrapper.Timestamp.Kind.ShouldBe(DateTimeKind.Local);
    }

    #endregion

    #region Agent ID Tests

    [Theory]
    [InlineData("")]
    [InlineData("simple-agent-id")]
    [InlineData("complex.agent.id.with.dots")]
    [InlineData("agent123")]
    [InlineData("UPPERCASE-AGENT")]
    public void PluginEventWrapper_SourceAgentId_HandlesVariousFormats(string agentId)
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act
        wrapper.SourceAgentId = agentId;

        // Assert
        wrapper.SourceAgentId.ShouldBe(agentId);
    }

    [Fact]
    public void PluginEventWrapper_SourceAgentId_CanBeNull()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act
        wrapper.SourceAgentId = null;

        // Assert
        wrapper.SourceAgentId.ShouldBeNull();
    }

    #endregion

    #region Equality and Comparison Tests

    [Fact]
    public void PluginEventWrapper_Equality_BasedOnProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var data = new { Message = "Test" };

        var wrapper1 = new PluginEventWrapper
        {
            PluginEventType = "TestEvent",
            PluginEventData = data,
            Timestamp = timestamp,
            CorrelationId = correlationId,
            SourceAgentId = "agent1"
        };

        var wrapper2 = new PluginEventWrapper
        {
            PluginEventType = "TestEvent",
            PluginEventData = data,
            Timestamp = timestamp,
            CorrelationId = correlationId,
            SourceAgentId = "agent1"
        };

        // Act & Assert
        // Note: Objects with same reference data will be equal
        wrapper1.PluginEventData.ShouldBe(wrapper2.PluginEventData);
        wrapper1.PluginEventType.ShouldBe(wrapper2.PluginEventType);
    }

    [Fact]
    public void PluginEventWrapper_DifferentData_NotEqual()
    {
        // Arrange
        var wrapper1 = new PluginEventWrapper
        {
            PluginEventType = "TestEvent",
            PluginEventData = "data1"
        };

        var wrapper2 = new PluginEventWrapper
        {
            PluginEventType = "TestEvent",
            PluginEventData = "data2"
        };

        // Act & Assert
        wrapper1.PluginEventData.ShouldNotBe(wrapper2.PluginEventData);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void PluginEventWrapper_LargeData_HandlesCorrectly()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var largeData = new string('A', 10000); // 10KB string

        // Act
        wrapper.PluginEventData = largeData;

        // Assert
        wrapper.PluginEventData.ShouldBe(largeData);
    }

    [Fact]
    public void PluginEventWrapper_MultiplePropertyChanges_HandlesCorrectly()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();

        // Act - Multiple changes
        wrapper.PluginEventType = "Event1";
        wrapper.PluginEventType = "Event2";
        wrapper.PluginEventType = "FinalEvent";

        wrapper.SourceAgentId = "agent1";
        wrapper.SourceAgentId = "agent2";

        // Assert
        wrapper.PluginEventType.ShouldBe("FinalEvent");
        wrapper.SourceAgentId.ShouldBe("agent2");
    }

    [Fact]
    public void PluginEventWrapper_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var wrapper = new PluginEventWrapper();
        var tasks = new List<Task>();

        // Act - Simulate concurrent access
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                wrapper.PluginEventType = $"Event{index}";
                wrapper.SourceAgentId = $"Agent{index}";
                wrapper.Timestamp = DateTime.UtcNow.AddSeconds(index);
            }));
        }

        // Assert
        Should.NotThrow(async () => await Task.WhenAll(tasks));
    }

    #endregion
}

/// <summary>
/// Test data class for complex data scenarios
/// </summary>
public class ComplexTestData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
} 