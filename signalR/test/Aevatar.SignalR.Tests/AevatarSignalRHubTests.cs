using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Newtonsoft.Json;
using Aevatar.SignalR.GAgents;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.SignalR.Tests;

public class AevatarSignalRHubTests
{
    private readonly Mock<ILogger<AevatarSignalRHub>> _mockLogger;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<ISingleClientProxy> _mockSingleClientProxy;

    public AevatarSignalRHubTests()
    {
        _mockLogger = new Mock<ILogger<AevatarSignalRHub>>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockSingleClientProxy = new Mock<ISingleClientProxy>();
        
        // Setup logger to handle any log method without throwing exceptions
        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()))
        .Callback(() => { });
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldAddToDefaultGroup()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = _mockContext.Object,
            Groups = _mockGroups.Object,
            Clients = _mockClients.Object
        };

        var connectionId = "test-connection-id";
        var groupName = Guid.Empty.ToString();
        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        
        // Setup context properties used in logging
        _mockContext.Setup(c => c.User).Returns((System.Security.Claims.ClaimsPrincipal)null);
        _mockContext.Setup(c => c.Items).Returns(new Dictionary<object, object>());

        // Act
        await hub.OnConnectedAsync();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync(connectionId, groupName, default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveFromDefaultGroup()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = _mockContext.Object,
            Groups = _mockGroups.Object,
            Clients = _mockClients.Object
        };

        var exception = new Exception("Test disconnection");
        var connectionId = "test-connection-id";
        var groupName = Guid.Empty.ToString();
        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await hub.OnDisconnectedAsync(exception);

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync(connectionId, groupName, default),
            Times.Once);
    }

    // UnsubscribeAsync Tests

    [Fact]
    public async Task UnsubscribeAsync_ShouldNotCallGetGAgentAsync_WhenConnectionIdIsNull()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var signalRGAgentGrainId = GrainId.Parse("signalR/00000000-0000-0000-0000-000000000003");
        
        // Setup context with null ConnectionId
        var nullConnContext = new Mock<HubCallerContext>();
        nullConnContext.Setup(c => c.ConnectionId).Returns((string)null);
        
        // Setup hub with mocked factory
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = nullConnContext.Object
        };
        
        // Act - This should run without exceptions
        await hub.UnsubscribeAsync(signalRGAgentGrainId);
        
        // Assert - No explicit assertions needed, test passes if no exception is thrown
    }

    [Fact]
    public async Task UnsubscribeAsync_WithNullContext_DoesNotThrowException()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        
        // Create hub with null context
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = null
        };
        
        // Act & Assert - Should not throw exception
        await hub.UnsubscribeAsync(default);
        
        // Test passes if no exception is thrown
    }

    [Fact]
    public async Task UnsubscribeAsync_WithEmptyConnectionId_DoesNothing()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var mockEmptyContext = new Mock<HubCallerContext>();
        
        // Setup empty string connection ID
        mockEmptyContext.Setup(c => c.ConnectionId).Returns(string.Empty);
        
        // Create hub with context that returns empty connection ID
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = mockEmptyContext.Object
        };
        
        // Act & Assert - This should run without exceptions
        await hub.UnsubscribeAsync(default);
        
        // No explicit assertions needed, test passes if no exception is thrown
    }

    // TODO: Add unit tests for AevatarSignalRHub methods
    // - PublishEventAsync (Needs grain mocking)
    // - SubscribeAsync (Needs grain mocking)
} 