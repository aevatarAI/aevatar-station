using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestEvents;
using Shouldly;
using Microsoft.Extensions.Logging;
using Aevatar.TestKit;
using Orleans;

namespace Aevatar.Core.Tests;

public class GAgentBasePlusTests : GAgentTestKitBase
{
    #region Basic Interface Tests

    [Fact(DisplayName = "GAgentBasePlus should implement IGAgentPlus interface")]
    public async Task GAgentBasePlus_ShouldImplementIGAgentPlusInterface()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Act & Assert
        var testValue = await agent.GetTestValueAsync();
        testValue.ShouldBe("IGAgentPlus-Test-Value");

        var description = await agent.GetDescriptionAsync(); 
        description.ShouldBe("Test IGAgentPlus Implementation");

        // Test IGAgentPlus specific methods
        var children = await agent.GetChildrenAsync();
        children.ShouldNotBeNull();
        
        var parents = await agent.GetParentsAsync();
        parents.ShouldNotBeNull();
    }

    [Fact(DisplayName = "GAgentBasePlus should implement IStateGAgentPlus interface")]
    public async Task GAgentBasePlus_ShouldImplementIStateGAgentPlusInterface()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Act
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestStatePlus>();
        state.TestValue.ShouldBe("IGAgentPlus-State");
    }

    #endregion

    #region Registration Tests

    [Fact(DisplayName = "RegisterAsync should establish bidirectional parent-child relationship")]
    public async Task RegisterAsync_ShouldEstablishBidirectionalRelationship()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Act
        await parent.RegisterAsync(child);

        // Assert
        var parentChildren = await parent.GetChildrenAsync();
        var childParents = await child.GetParentsAsync();

        parentChildren.ShouldContain(child.GetGrainId());
        childParents.ShouldContain(parent.GetGrainId());
    }

    [Fact(DisplayName = "RegisterManyAsync should register multiple children efficiently")]
    public async Task RegisterManyAsync_ShouldRegisterMultipleChildren()
    {
        // Arrange - Create grains with explicit unique GUIDs to avoid test framework conflicts
        var parentGuid = Guid.NewGuid();
        var child1Guid = Guid.NewGuid(); 
        var child2Guid = Guid.NewGuid();
        var child3Guid = Guid.NewGuid();

        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(parentGuid);
        var child1 = await Silo.CreateGrainAsync<TestIGAgentPlus>(child1Guid);
        var child2 = await Silo.CreateGrainAsync<TestIGAgentPlus>(child2Guid);
        var child3 = await Silo.CreateGrainAsync<TestIGAgentPlus>(child3Guid);
        
        // Activate all grains to ensure consistent state
        await parent.ActivateAsync();
        await child1.ActivateAsync();
        await child2.ActivateAsync();
        await child3.ActivateAsync();
        
        var children = new List<IGAgentPlus> { child1, child2, child3 };

        // Act
        await parent.RegisterManyAsync(children);

        // Assert
        var parentChildren = await parent.GetChildrenAsync();
        parentChildren.Count.ShouldBe(3);
        parentChildren.ShouldContain(child1.GetGrainId());
        parentChildren.ShouldContain(child2.GetGrainId());
        parentChildren.ShouldContain(child3.GetGrainId());

        // Each child should have parent registered
        var child1Parents = await child1.GetParentsAsync();
        var child2Parents = await child2.GetParentsAsync();
        var child3Parents = await child3.GetParentsAsync();

        child1Parents.ShouldContain(parent.GetGrainId());
        child2Parents.ShouldContain(parent.GetGrainId());
        child3Parents.ShouldContain(parent.GetGrainId());
    }

    [Fact(DisplayName = "RegisterAsync should prevent self-registration")]
    public async Task RegisterAsync_ShouldPreventSelfRegistration()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Act
        await agent.RegisterAsync(agent);

        // Assert - Should not add itself as child
        var children = await agent.GetChildrenAsync();
        children.ShouldBeEmpty();
    }

    #endregion

    #region Subscription Management Tests

    [Fact(DisplayName = "SubscribeToParentAsync should establish parent subscription only")]
    public async Task SubscribeToParentAsync_ShouldEstablishSubscription()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Act
        await child.SubscribeToParentAsync(parent);
        
        // Assert - Verify subscription handler is registered in State.Subscription (NOT parent relationship)
        var childSubscriptions = await child.GetSubscriptionHandlesAsync();
        childSubscriptions.ShouldNotBeNull();
        
        // Verify that there is a subscription for the parent's downward stream
        var expectedStreamKey = $"{parent.GetGrainId()}.1.EventBase"; // parent.direction.eventType
        childSubscriptions.Keys.ShouldContain(k => k.Contains(parent.GetGrainId().ToString()) && k.Contains(".1."));
        
        // Verify that NO parent relationship is established (subscription != relationship)
        var childParents = await child.GetParentsAsync();
        childParents.ShouldNotContain(parent.GetGrainId());
    }

    [Fact(DisplayName = "UnsubscribeFromParentAsync should remove parent subscription only")]
    public async Task UnsubscribeFromParentAsync_ShouldRemoveSubscription()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        await child.SubscribeToParentAsync(parent);

        // Verify subscription was established
        var subscriptionsBefore = await child.GetSubscriptionHandlesAsync();
        subscriptionsBefore.Keys.ShouldContain(k => k.Contains(parent.GetGrainId().ToString()) && k.Contains(".1."));

        // Act
        await child.UnsubscribeFromParentAsync(parent);
        
        // Assert - Verify subscription handler is removed from State.Subscription (NOT parent relationship)
        var subscriptionsAfter = await child.GetSubscriptionHandlesAsync();
        subscriptionsAfter.Keys.ShouldNotContain(k => k.Contains(parent.GetGrainId().ToString()) && k.Contains(".1."));
        
        // Verify that parent relationship is NOT affected (unsubscription != relationship removal)
        var childParents = await child.GetParentsAsync();
        childParents.ShouldBeEmpty(); // Was empty before since SubscribeToParentAsync doesn't create relationships
    }

    [Fact(DisplayName = "SubscribeToManyParentAsync should subscribe child to parent streams for event forwarding")]
    public async Task SubscribeToManyParentAsync_ShouldHandleBatchSubscriptions()
    {
        // Arrange
        var parent1 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var parent2 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var parent3 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        await child.ActivateAsync();
        await parent1.ActivateAsync();
        await parent2.ActivateAsync();
        await parent3.ActivateAsync();

        var parents = new List<IGAgentPlus> { parent1, parent2, parent3 };

        // Get subscription state before operation
        var subscriptionsBefore = await child.GetSubscriptionHandlesAsync();
        
        // Act - Subscribe child to all parent streams
        await child.SubscribeToManyParentAsync(parents);

        // Allow time for Orleans stream subscriptions to be established
        await Task.Delay(100);

        // Assert - Verify subscription handlers are registered in State.Subscription
        var subscriptionsAfter = await child.GetSubscriptionHandlesAsync();
        subscriptionsAfter.ShouldNotBeNull();
        
        // Verify that subscription count increased (should have new subscriptions for each parent)
        subscriptionsAfter.Count.ShouldBeGreaterThan(subscriptionsBefore.Count);
        
        // Verify specific parent stream subscriptions exist (downward direction = 1)
        subscriptionsAfter.Keys.ShouldContain(k => k.Contains(parent1.GetGrainId().ToString()) && k.Contains(".1."));
        subscriptionsAfter.Keys.ShouldContain(k => k.Contains(parent2.GetGrainId().ToString()) && k.Contains(".1."));
        subscriptionsAfter.Keys.ShouldContain(k => k.Contains(parent3.GetGrainId().ToString()) && k.Contains(".1."));
        
        // Verify that the child agent is now subscribed to event handlers
        var subscribedEvents = await child.GetAllSubscribedEventsAsync();
        subscribedEvents.ShouldNotBeNull();
        subscribedEvents.ShouldNotBeEmpty();
        
        // Verify that the child can handle the event type used for forwarding
        subscribedEvents.ShouldContain(typeof(EventBase));
        
        // Test that subscriptions are working by having each parent publish a downward event
        var testEvent1 = new NaiveTestEvent { Greeting = "From Parent 1", Direction = EventDirection.Down };
        var testEvent2 = new NaiveTestEvent { Greeting = "From Parent 2", Direction = EventDirection.Down };
        var testEvent3 = new NaiveTestEvent { Greeting = "From Parent 3", Direction = EventDirection.Down };

        // Publishing events should not throw exceptions if subscriptions are properly set up
        await Should.NotThrowAsync(async () => await parent1.PublishEventByDirectionAsync(testEvent1));
        await Should.NotThrowAsync(async () => await parent2.PublishEventByDirectionAsync(testEvent2));
        await Should.NotThrowAsync(async () => await parent3.PublishEventByDirectionAsync(testEvent3));

        // Allow time for event processing
        await Task.Delay(200);
        
        // Verify that subscription operation completed successfully
        var childState = await child.GetStateAsync();
        childState.ShouldNotBeNull();
        
        // Verify that no parent relationships were created (subscriptions != relationships)
        var childParents = await child.GetParentsAsync();
        childParents.ShouldBeEmpty(); // Stream subscriptions don't affect parent/child state relationships
    }

    #endregion

    #region Unregistration Tests

    [Fact(DisplayName = "UnregisterAsync should clean up bidirectional relationships")]
    public async Task UnregisterAsync_ShouldCleanupBidirectionalRelationships()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        await parent.RegisterAsync(child);

        // Verify registration worked
        var parentChildrenBefore = await parent.GetChildrenAsync();
        var childParentsBefore = await child.GetParentsAsync();
        parentChildrenBefore.ShouldContain(child.GetGrainId());
        childParentsBefore.ShouldContain(parent.GetGrainId());

        // Act
        await parent.UnregisterAsync(child);

        // Assert
        var parentChildrenAfter = await parent.GetChildrenAsync();
        var childParentsAfter = await child.GetParentsAsync();
        parentChildrenAfter.ShouldNotContain(child.GetGrainId());
        childParentsAfter.ShouldNotContain(parent.GetGrainId());
    }

    [Fact(DisplayName = "UnregisterParentAsync should remove parent relationship")]
    public async Task UnregisterParentAsync_ShouldRemoveParentRelationship()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        await parent.RegisterAsync(child);

        // Act
        await child.UnregisterParentAsync(parent);

        // Assert
        var parentChildren = await parent.GetChildrenAsync();
        var childParents = await child.GetParentsAsync();
        parentChildren.ShouldNotContain(child.GetGrainId());
        childParents.ShouldNotContain(parent.GetGrainId());
    }

    #endregion

    #region Event Publishing Tests

    [Fact(DisplayName = "PublishEventByDirectionAsync should handle different event directions")]
    public async Task PublishEventByDirectionAsync_ShouldHandleEventDirections()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        var upEvent = new NaiveTestEvent { Greeting = "Up", Direction = EventDirection.Up };
        var downEvent = new NaiveTestEvent { Greeting = "Down", Direction = EventDirection.Down };
        var bidirectionalEvent = new NaiveTestEvent { Greeting = "Bidirectional", Direction = EventDirection.Bidirectional };
        var upThenDownEvent = new NaiveTestEvent { Greeting = "UpThenDown", Direction = EventDirection.UpThenDown };

        // Act & Assert - Should not throw exceptions
        await Should.NotThrowAsync(async () => await agent.PublishEventByDirectionAsync(upEvent));
        await Should.NotThrowAsync(async () => await agent.PublishEventByDirectionAsync(downEvent));
        await Should.NotThrowAsync(async () => await agent.PublishEventByDirectionAsync(bidirectionalEvent));
        await Should.NotThrowAsync(async () => await agent.PublishEventByDirectionAsync(upThenDownEvent));

        // Verify publisher grain ID is set
        upEvent.PublisherGrainId.ShouldBe(agent.GetGrainId());
        downEvent.PublisherGrainId.ShouldBe(agent.GetGrainId());
        bidirectionalEvent.PublisherGrainId.ShouldBe(agent.GetGrainId());
        upThenDownEvent.PublisherGrainId.ShouldBe(agent.GetGrainId());
    }

    #endregion

    #region State Management Tests

    [Fact(DisplayName = "GetChildrenAsync should return all registered children")]
    public async Task GetChildrenAsync_ShouldReturnAllChildren()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child1 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child2 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child3 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        await parent.RegisterAsync(child1);
        await parent.RegisterAsync(child2);
        await parent.RegisterAsync(child3);

        // Act
        var children = await parent.GetChildrenAsync();

        // Assert
        children.Count.ShouldBe(3);
        children.ShouldContain(child1.GetGrainId());
        children.ShouldContain(child2.GetGrainId());
        children.ShouldContain(child3.GetGrainId());
    }

    [Fact(DisplayName = "GetParentsAsync should return all registered parents")]
    public async Task GetParentsAsync_ShouldReturnAllParents()
    {
        // Arrange
        var parent1 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var parent2 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Use AddParentAsync to establish actual parent relationships (not just subscriptions)
        await child.AddParentAsync(parent1.GetGrainId());
        await child.AddParentAsync(parent2.GetGrainId());

        // Act
        var parents = await child.GetParentsAsync();

        // Assert
        parents.Count.ShouldBe(2);
        parents.ShouldContain(parent1.GetGrainId());
        parents.ShouldContain(parent2.GetGrainId());
    }

    [Fact(DisplayName = "RemoveSpecificParentAsync should remove specific parent")]
    public async Task RemoveSpecificParentAsync_ShouldRemoveSpecificParent()
    {
        // Arrange
        var parent1 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var parent2 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        // Use AddParentAsync to establish actual parent relationships
        await child.AddParentAsync(parent1.GetGrainId());
        await child.AddParentAsync(parent2.GetGrainId());

        // Act
        await child.RemoveParentAsync(parent1.GetGrainId());

        // Assert
        var parents = await child.GetParentsAsync();
        parents.ShouldNotContain(parent1.GetGrainId());
        parents.ShouldContain(parent2.GetGrainId());
    }

    [Fact(DisplayName = "RemoveSpecificChildAsync should remove specific child")]
    public async Task RemoveSpecificChildAsync_ShouldRemoveSpecificChild()
    {
        // Arrange
        var parent = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child1 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());
        var child2 = await Silo.CreateGrainAsync<TestIGAgentPlus>(Guid.NewGuid());

        await parent.RegisterAsync(child1);
        await parent.RegisterAsync(child2);

        // Act
        await parent.RemoveChildAsync(child1.GetGrainId());

        // Assert
        var children = await parent.GetChildrenAsync();
        children.ShouldNotContain(child1.GetGrainId());
        children.ShouldContain(child2.GetGrainId());
    }

    #endregion
}
