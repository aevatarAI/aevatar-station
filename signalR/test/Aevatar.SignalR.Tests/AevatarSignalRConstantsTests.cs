using Shouldly;
using Xunit;

namespace Aevatar.SignalR.Tests;

public class AevatarSignalRConstantsTests
{
    [Fact]
    public void AevatarSignalRStreamNamespaces_ShouldContainExpectedValues()
    {
        // Arrange & Act
        var namespaces = AevatarSignalRConstants.AevatarSignalRStreamNamespaces;
        
        // Assert
        namespaces.ShouldContain("ServerDisconnectStream");
        namespaces.ShouldContain("ServerStream");
        namespaces.ShouldContain("ClientDisconnectStream");
        namespaces.ShouldContain("AllStream");
    }
    
    [Fact]
    public void AevatarSignalRStreamNamespaces_ShouldContainFourElements()
    {
        // Arrange & Act
        var namespaces = AevatarSignalRConstants.AevatarSignalRStreamNamespaces;
        
        // Assert
        namespaces.Length.ShouldBe(4);
    }
    
    [Fact]
    public void AevatarSignalRStreamNamespaces_ElementsInExpectedOrder()
    {
        // Arrange & Act
        var namespaces = AevatarSignalRConstants.AevatarSignalRStreamNamespaces;
        
        // Assert
        namespaces[0].ShouldBe("ServerDisconnectStream");
        namespaces[1].ShouldBe("ServerStream");
        namespaces[2].ShouldBe("ClientDisconnectStream");
        namespaces[3].ShouldBe("AllStream");
    }
} 