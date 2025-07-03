using Shouldly;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class ClientMessageTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var hubName = "TestHub";
        var connectionId = "TestConnection";
        var notification = new ClientNotification("testTarget", new[] { "arg1", "arg2" });
        
        // Act
        var clientMessage = new ClientMessage(hubName, connectionId, notification);
        
        // Assert
        clientMessage.HubName.ShouldBe(hubName);
        clientMessage.ConnectionId.ShouldBe(connectionId);
        clientMessage.Message.ShouldBeSameAs(notification);
    }
    
    [Fact]
    public void EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var notification1 = new ClientNotification("testTarget", new[] { "arg1" });
        
        var message1 = new ClientMessage("Hub1", "Connection1", notification1);
        var message2 = new ClientMessage("Hub1", "Connection1", notification1);
        var message3 = new ClientMessage("Hub2", "Connection1", notification1);
        
        // Act & Assert - Same values should be equal
        message1.Equals(message2).ShouldBeTrue();
        (message1 == message2).ShouldBeTrue();
        
        // Act & Assert - Different values should not be equal
        message1.Equals(message3).ShouldBeFalse();
        (message1 == message3).ShouldBeFalse();
    }
    
    [Fact]
    public void WithMethods_ShouldCreateNewInstanceWithModifiedProperty()
    {
        // Arrange
        var notification1 = new ClientNotification("testTarget", new[] { "arg1" });
        var notification2 = new ClientNotification("newTarget", new[] { "arg2" });
        var original = new ClientMessage("Hub1", "Connection1", notification1);
        
        // Act - Create new instances with modified properties
        var withNewHub = original with { HubName = "Hub2" };
        var withNewConnection = original with { ConnectionId = "Connection2" };
        var withNewMessage = original with { Message = notification2 };
        
        // Assert - New instances have modified properties
        withNewHub.HubName.ShouldBe("Hub2");
        withNewHub.ConnectionId.ShouldBe(original.ConnectionId);
        withNewHub.Message.ShouldBe(original.Message);
        
        withNewConnection.HubName.ShouldBe(original.HubName);
        withNewConnection.ConnectionId.ShouldBe("Connection2");
        withNewConnection.Message.ShouldBe(original.Message);
        
        withNewMessage.HubName.ShouldBe(original.HubName);
        withNewMessage.ConnectionId.ShouldBe(original.ConnectionId);
        withNewMessage.Message.ShouldBe(notification2);
        
        // Assert - Original remains unchanged
        original.HubName.ShouldBe("Hub1");
        original.ConnectionId.ShouldBe("Connection1");
        original.Message.ShouldBe(notification1);
    }
} 