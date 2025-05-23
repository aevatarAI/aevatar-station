using Shouldly;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class AevatarSignalROptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultTopicPrefix()
    {
        // Arrange & Act
        var options = new AevatarSignalROptions();
        
        // Assert
        options.TopicPrefix.ShouldBe("Aevatar");
    }
    
    [Fact]
    public void TopicPrefix_Property_ShouldSetAndGetValue()
    {
        // Arrange
        var options = new AevatarSignalROptions();
        var customPrefix = "CustomPrefix";
        
        // Act
        options.TopicPrefix = customPrefix;
        
        // Assert
        options.TopicPrefix.ShouldBe(customPrefix);
    }
    
    [Fact]
    public void TopicPrefix_Property_ShouldAllowNullValue()
    {
        // Arrange
        var options = new AevatarSignalROptions();
        
        // Act
        options.TopicPrefix = null;
        
        // Assert
        options.TopicPrefix.ShouldBeNull();
    }
} 