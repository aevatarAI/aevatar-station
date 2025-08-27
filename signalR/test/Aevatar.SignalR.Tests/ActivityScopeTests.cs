using System.Diagnostics;
using Shouldly;

namespace Aevatar.SignalR.Tests;

public class ActivityScopeTests
{
    [Fact]
    public void Constructor_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        var operationName = "TestOperation";
        var previousActivity = Activity.Current;
        
        // Act
        using (var scope = new ActivityScope(operationName))
        {
            // Assert
            Activity.Current.ShouldNotBeNull();
            Activity.Current.OperationName.ShouldBe(operationName);
        }
        
        // Verify activity is cleaned up
        Activity.Current.ShouldBe(previousActivity);
    }
    
    [Fact]
    public void Dispose_ShouldEndCurrentActivity()
    {
        // Arrange
        var operationName = "TestOperation";
        var previousActivity = Activity.Current;
        var scope = new ActivityScope(operationName);
        
        // Verify activity is created
        Activity.Current.ShouldNotBeNull();
        
        // Act
        scope.Dispose();
        
        // Assert - activity should be ended
        Activity.Current.ShouldBe(previousActivity);
    }
    
    [Fact]
    public void Scopes_ShouldNestProperly()
    {
        // Arrange
        var outerOperationName = "OuterOperation";
        var innerOperationName = "InnerOperation";
        var previousActivity = Activity.Current;
        
        // Act & Assert - test nested scopes
        using (var outerScope = new ActivityScope(outerOperationName))
        {
            Activity.Current.ShouldNotBeNull();
            Activity.Current.OperationName.ShouldBe(outerOperationName);
            
            using (var innerScope = new ActivityScope(innerOperationName))
            {
                Activity.Current.ShouldNotBeNull();
                Activity.Current.OperationName.ShouldBe(innerOperationName);
                Activity.Current.Parent.ShouldNotBeNull();
                Activity.Current.Parent.OperationName.ShouldBe(outerOperationName);
            }
            
            // Inner activity should be ended
            Activity.Current.ShouldNotBeNull();
            Activity.Current.OperationName.ShouldBe(outerOperationName);
        }
        
        // Both activities should be ended
        Activity.Current.ShouldBe(previousActivity);
    }
} 