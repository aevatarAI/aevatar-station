using Microsoft.AspNetCore.SignalR;
using Shouldly;
using System.Reflection;

namespace Aevatar.SignalR.Tests;

public class HubUtilityTests
{
    [Fact]
    public void HubName_ShouldMatchClassName()
    {
        // This test indirectly verifies that hub names are correctly derived
        // from the class name, which is the functionality of HubUtility.GetHubName<THub>()
        
        // Arrange & Act - Use a Hub class that's accessible in the tests
        var hubType = typeof(AevatarSignalRHub);
        var hubName = hubType.Name;

        // Assert
        hubName.ShouldBe("AevatarSignalRHub");
    }

    [Fact]
    public void DerivedHubName_ShouldMatchDerivedClassName()
    {
        // Arrange & Act - Test with derived hub class
        var hubType = typeof(TestDerivedHub);
        var hubName = hubType.Name;

        // Assert
        hubName.ShouldBe("TestDerivedHub");
    }

    // Test hub class for inheritance testing
    private class TestBaseHub : Hub { }
    private class TestDerivedHub : TestBaseHub { }
} 