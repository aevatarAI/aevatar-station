using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Tests.Extensions;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace Aevatar.SignalR.Tests;

public class EventDeserializerTests : AevatarSignalRTestBase
{
    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new EventDeserializer());
    }

    [Fact]
    public void DeserializeEvent_WithValidEventType_ShouldDeserializeCorrectly()
    {
        // Arrange
        var deserializer = new EventDeserializer();
        var testEvent = new NaiveTestEvent { Greeting = "Hello, Test!" };
        var eventJson = JsonConvert.SerializeObject(testEvent);
        var eventTypeName = typeof(NaiveTestEvent).FullName!;

        // Act
        var result = deserializer.DeserializeEvent(eventJson, eventTypeName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<NaiveTestEvent>();
        var naiveTestEvent = (NaiveTestEvent)result;
        naiveTestEvent.Greeting.ShouldBe("Hello, Test!");
    }

    [Fact]
    public void DeserializeEvent_WithInvalidEventType_ShouldThrowException()
    {
        // Arrange
        var deserializer = new EventDeserializer();
        var eventJson = "{}";
        var nonExistentEventTypeName = "NonExistentEvent";

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => 
            deserializer.DeserializeEvent(eventJson, nonExistentEventTypeName));
    }

    [Fact]
    public void DeserializeEvent_WithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var deserializer = new EventDeserializer();
        var invalidJson = "{invalid_json}";
        var eventTypeName = typeof(NaiveTestEvent).FullName!;

        // Act & Assert
        Should.Throw<JsonException>(() => 
            deserializer.DeserializeEvent(invalidJson, eventTypeName));
    }
} 