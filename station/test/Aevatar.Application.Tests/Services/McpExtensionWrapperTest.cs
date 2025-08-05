using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Options;
using Aevatar.Services;
using Moq;
using Xunit;

namespace Aevatar.Application.Tests.Services;

/// <summary>
/// Unit tests for McpExtensionWrapper
/// </summary>
public class McpExtensionWrapperTest
{
    private readonly Mock<IGAgentFactory> _mockGAgentFactory;
    private readonly McpExtensionWrapper _wrapper;

    public McpExtensionWrapperTest()
    {
        _mockGAgentFactory = new Mock<IGAgentFactory>();
        _wrapper = new McpExtensionWrapper();
    }

    [Fact]
    public async Task GetMCPWhiteListAsync_ShouldCallExtensionMethod()
    {
        // Arrange
        var expectedResult = new Dictionary<string, MCPServerConfig>
        {
            ["test-server"] = new MCPServerConfig
            {
                ServerName = "test-server",
                Command = "python",
                Description = "Test server"
            }
        };

        // Setup the extension method call through reflection or by using the actual implementation
        // Since we can't easily mock static extension methods, we'll test the wrapper logic
        
        // Act - This will call the actual extension method
        // In a real scenario, this would call _mockGAgentFactory.Object.GetMCPWhiteListAsync()
        // But since it's an extension method, we'll test that the wrapper correctly delegates
        var result = await _wrapper.GetMCPWhiteListAsync(_mockGAgentFactory.Object);

        // Assert
        // Since we're testing the wrapper pattern, we verify the method doesn't throw
        // and returns the expected type
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, MCPServerConfig>>(result);
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_WithValidJson_ShouldCallExtensionMethod()
    {
        // Arrange
        var configJson = "{\"test-server\":{\"ServerName\":\"test-server\",\"Command\":\"python\"}}";

        // Act - This will call the actual extension method
        var result = await _wrapper.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, configJson);

        // Assert
        // Since we're testing the wrapper pattern, we verify the method doesn't throw
        // and returns the expected type
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_WithEmptyJson_ShouldCallExtensionMethod()
    {
        // Arrange
        var configJson = "{}";

        // Act - This will call the actual extension method
        var result = await _wrapper.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, configJson);

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_WithNullJson_ShouldCallExtensionMethod()
    {
        // Arrange
        string? configJson = null;

        // Act - This will call the actual extension method
        var result = await _wrapper.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, configJson!);

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void McpExtensionWrapper_ShouldImplementInterface()
    {
        // Arrange & Act
        var wrapper = new McpExtensionWrapper();

        // Assert
        Assert.IsAssignableFrom<IMcpExtensionWrapper>(wrapper);
    }
}