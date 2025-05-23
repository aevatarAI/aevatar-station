using Shouldly;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class AevatarStreamConfigTests : IDisposable
{
    private readonly string _originalPrefix;

    public AevatarStreamConfigTests()
    {
        // Store the original prefix to restore after tests
        _originalPrefix = AevatarStreamConfig.Prefix;
    }

    public void Dispose()
    {
        // Restore the original prefix after each test
        AevatarStreamConfig.Initialize(_originalPrefix);
    }

    [Fact]
    public void Default_Prefix_ShouldBeDefaultTopicPrefix()
    {
        // Arrange - Reset to default state
        AevatarStreamConfig.Initialize(SignalROrleansConstants.DefaultTopicPrefix);
        
        // Act & Assert
        AevatarStreamConfig.Prefix.ShouldBe(SignalROrleansConstants.DefaultTopicPrefix);
        AevatarStreamConfig.Prefix.ShouldBe("AevatarSignalR");
    }

    [Fact]
    public void Initialize_WithValidPrefix_ShouldUpdatePrefix()
    {
        // Arrange
        var newPrefix = "TestPrefix";
        
        // Act
        AevatarStreamConfig.Initialize(newPrefix);
        
        // Assert
        AevatarStreamConfig.Prefix.ShouldBe(newPrefix);
    }

    [Fact]
    public void Initialize_WithNullPrefix_ShouldNotChangePrefix()
    {
        // Arrange
        var knownPrefix = "KnownPrefix";
        AevatarStreamConfig.Initialize(knownPrefix);
        
        // Act
        AevatarStreamConfig.Initialize(null);
        
        // Assert
        AevatarStreamConfig.Prefix.ShouldBe(knownPrefix);
    }
} 