using Shouldly;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class ClientNotificationTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var target = "testTarget";
        var args = new[] { "arg1", "arg2", "arg3" };

        // Act
        var notification = new ClientNotification(target, args);

        // Assert
        notification.Target.ShouldBe(target);
        notification.Arguments.ShouldBeSameAs(args);
    }

    [Fact]
    public void Constructor_WithEmptyValues_ShouldInitializeCorrectly()
    {
        // Arrange
        var target = string.Empty;
        var args = Array.Empty<string>();

        // Act
        var notification = new ClientNotification(target, args);

        // Assert
        notification.Target.ShouldBe(string.Empty);
        notification.Arguments.ShouldBe(Array.Empty<string>());
    }
    
    [Fact]
    public void Args_PropertyShouldBeInitializedAsNull()
    {
        // Arrange & Act
        var notification = new ClientNotification("target", new[] { "arg" });
        
        // Assert
        notification.Args.ShouldBeNull(); // Verify Args property initialization state
    }
} 