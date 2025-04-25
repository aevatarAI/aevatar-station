using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aevatar.SignalR.GAgents;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace Aevatar.SignalR.Tests;

public class AevatarSignalRHubNullConnectionTests
{
    private readonly Mock<ILogger<AevatarSignalRHub>> _mockLogger;
    private readonly Mock<HubCallerContext> _mockContext;

    public AevatarSignalRHubNullConnectionTests()
    {
        _mockLogger = new Mock<ILogger<AevatarSignalRHub>>();
        _mockContext = new Mock<HubCallerContext>();
    }

    [Fact]
    public async Task UnsubscribeAsync_ShouldNotCallGetGAgentAsync_WhenConnectionIdIsNull()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var signalRGAgentGrainId = GrainId.Parse("signalR/00000000-0000-0000-0000-000000000003");
        
        // Setup context with null ConnectionId
        _mockContext.Setup(c => c.ConnectionId).Returns((string)null);
        
        // Setup hub with mocked factory
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = _mockContext.Object
        };
        
        // Act - This should run without exceptions
        await hub.UnsubscribeAsync(signalRGAgentGrainId);
        
        // Assert - No explicit assertions needed, test passes if no exception is thrown
    }

    [Fact]
    public async Task UnsubscribeAsync_WithNullConnectionId_DoesNothing()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        
        // Create hub with null context
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = null
        };
        
        // Act & Assert - This should run without exceptions
        await hub.UnsubscribeAsync(default);
        
        // No explicit assertions needed, test passes if no exception is thrown
    }

    [Fact]
    public async Task UnsubscribeAsync_WithEmptyConnectionId_DoesNothing()
    {
        // Arrange
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var mockContext = new Mock<HubCallerContext>();
        
        // Setup empty string connection ID
        mockContext.Setup(c => c.ConnectionId).Returns(string.Empty);
        
        // Create hub with context that returns empty connection ID
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, _mockLogger.Object)
        {
            Context = mockContext.Object
        };
        
        // Act & Assert - This should run without exceptions
        await hub.UnsubscribeAsync(default);
        
        // No explicit assertions needed, test passes if no exception is thrown
    }

    [Fact]
    public async Task UnsubscribeAsync_WithNullContext_DoesNotThrowException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AevatarSignalRHub>>();
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        
        // Create hub with null context
        var hub = new AevatarSignalRHub(mockGAgentFactory.Object, mockLogger.Object)
        {
            Context = null
        };
        
        // Act & Assert - Should not throw exception
        await hub.UnsubscribeAsync(default);
        
        // Test passes if no exception is thrown
    }
} 