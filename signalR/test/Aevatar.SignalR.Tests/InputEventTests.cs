using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class InputEventTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange & Act
        var inputEvent = new InputEvent();

        // Assert
        inputEvent.ShouldNotBeNull();
        inputEvent.ShouldBeOfType<InputEvent>();
        inputEvent.ShouldBeAssignableTo<EventBase>();
    }

    [Fact]
    public void Message_Property_ShouldSetAndGetValue()
    {
        // Arrange
        var inputEvent = new InputEvent();
        var testMessage = "This is a test message";

        // Act
        inputEvent.Message = testMessage;

        // Assert
        inputEvent.Message.ShouldBe(testMessage);
    }

    [Fact]
    public void Message_Property_ShouldAllowNullValue()
    {
        // Arrange
        var inputEvent = new InputEvent();
        
        // Act
        inputEvent.Message = null;

        // Assert
        inputEvent.Message.ShouldBeNull();
    }
} 