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

    // TODO: Add unit tests for AevatarSignalRHub methods
    // - PublishEventAsync (Needs grain mocking)
    // - SubscribeAsync (Needs grain mocking)
    // - UnsubscribeAsync (Needs grain mocking)
} 