using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Options;
using Aevatar.Mcp;
using Aevatar.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp;
using Xunit;

namespace Aevatar.Application.Tests.Controllers;

/// <summary>
/// Unit tests for the real McpServerController using wrapper pattern
/// </summary>
public class McpServerControllerRealTest
{
    private readonly Mock<IGAgentFactory> _mockGAgentFactory;
    private readonly Mock<ILogger<McpServerController>> _mockLogger;
    private readonly Mock<IMcpExtensionWrapper> _mockMcpExtensionWrapper;
    private readonly McpServerController _controller;

    public McpServerControllerRealTest()
    {
        _mockGAgentFactory = new Mock<IGAgentFactory>();
        _mockLogger = new Mock<ILogger<McpServerController>>();
        _mockMcpExtensionWrapper = new Mock<IMcpExtensionWrapper>();
        
        _controller = new McpServerController(
            _mockLogger.Object,
            _mockGAgentFactory.Object,
            _mockMcpExtensionWrapper.Object);
    }

    #region GetListAsync Tests

    [Fact]
    public async Task GetListAsync_WithValidInput_ShouldReturnPagedResult()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server1"] = new MCPServerConfig
            {
                ServerName = "server1",
                Command = "python",
                Description = "Test server 1"
            },
            ["server2"] = new MCPServerConfig
            {
                ServerName = "server2",
                Command = "node",
                Description = "Test server 2"
            }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("server1", result.Items.First().ServerName);
        Assert.Equal("python", result.Items.First().Command);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_WithServerNameFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            ServerName = "test",
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["test-server"] = new MCPServerConfig
            {
                ServerName = "test-server",
                Command = "python",
                Description = "Test server"
            },
            ["other-server"] = new MCPServerConfig
            {
                ServerName = "other-server",
                Command = "node",
                Description = "Other server"
            }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("test-server", result.Items.First().ServerName);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_WithSearchTerm_ShouldReturnMatchingResults()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            SearchTerm = "python",
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server1"] = new MCPServerConfig
            {
                ServerName = "server1",
                Command = "python",
                Description = "Python server"
            },
            ["server2"] = new MCPServerConfig
            {
                ServerName = "server2",
                Command = "node",
                Description = "Node server"
            }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("server1", result.Items.First().ServerName);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task GetListAsync_WithInvalidPageSize_ShouldThrowException(int pageSize)
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            MaxResultCount = pageSize,
            SkipCount = 0
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.GetListAsync(input));
        Assert.Contains("Page size must be between 1 and 100", exception.Message);
    }

    [Fact]
    public async Task GetListAsync_WithNegativeSkipCount_ShouldThrowException()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            MaxResultCount = 10,
            SkipCount = -1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.GetListAsync(input));
        Assert.Contains("Skip count cannot be negative", exception.Message);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithExistingServer_ShouldReturnServer()
    {
        // Arrange
        var serverName = "test-server";
        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            [serverName] = new MCPServerConfig
            {
                ServerName = serverName,
                Command = "python",
                Description = "Test server"
            }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetAsync(serverName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverName, result.ServerName);
        Assert.Equal("python", result.Command);
        Assert.Equal("Test server", result.Description);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentServer_ShouldThrowException()
    {
        // Arrange
        var serverName = "non-existent-server";
        var mockConfigs = new Dictionary<string, MCPServerConfig>();

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.GetAsync(serverName));
        Assert.Contains($"MCP server '{serverName}' not found", exception.Message);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAsync_WithInvalidServerName_ShouldThrowException(string serverName)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.GetAsync(serverName));
        Assert.Contains("Server name is required", exception.Message);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidInput_ShouldCreateServer()
    {
        // Arrange
        var input = new CreateMcpServerDto
        {
            ServerName = "test-server",
            Command = "python",
            Description = "Test server"
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>();
        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);
        _mockMcpExtensionWrapper.Setup(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(input.ServerName, result.ServerName);
        Assert.Equal(input.Command, result.Command);
        Assert.Equal(input.Description, result.Description);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateServerName_ShouldThrowException()
    {
        // Arrange
        var input = new CreateMcpServerDto
        {
            ServerName = "existing-server",
            Command = "python"
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["existing-server"] = new MCPServerConfig { ServerName = "existing-server" }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(input));
        Assert.Contains("MCP server 'existing-server' already exists", exception.Message);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullInput_ShouldThrowException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(null));
        Assert.Contains("Invalid input data", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateAsync_WithInvalidServerName_ShouldThrowException(string serverName)
    {
        // Arrange
        var input = new CreateMcpServerDto
        {
            ServerName = serverName,
            Command = "python"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(input));
        Assert.Contains("Server name is required", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateAsync_WithInvalidCommand_ShouldThrowException(string command)
    {
        // Arrange
        var input = new CreateMcpServerDto
        {
            ServerName = "test-server",
            Command = command
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(input));
        Assert.Contains("Server command is required", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithConfigurationFailure_ShouldThrowException()
    {
        // Arrange
        var input = new CreateMcpServerDto
        {
            ServerName = "test-server",
            Command = "python"
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>();
        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);
        _mockMcpExtensionWrapper.Setup(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(input));
        Assert.Contains("Failed to create MCP server configuration", exception.Message);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidInput_ShouldUpdateServer()
    {
        // Arrange
        var serverName = "test-server";
        var input = new UpdateMcpServerDto
        {
            Command = "node",
            Description = "Updated description"
        };

        var existingConfig = new MCPServerConfig
        {
            ServerName = serverName,
            Command = "python",
            Description = "Original description"
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            [serverName] = existingConfig
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);
        _mockMcpExtensionWrapper.Setup(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateAsync(serverName, input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverName, result.ServerName);
        Assert.Equal("node", result.Command); // Updated
        Assert.Equal("Updated description", result.Description); // Updated
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentServer_ShouldThrowException()
    {
        // Arrange
        var serverName = "non-existent-server";
        var input = new UpdateMcpServerDto
        {
            Command = "python"
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>();
        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.UpdateAsync(serverName, input));
        Assert.Contains($"MCP server '{serverName}' not found", exception.Message);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullInput_ShouldThrowException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.UpdateAsync("test-server", null));
        Assert.Contains("Invalid input data", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingServer_ShouldDeleteServer()
    {
        // Arrange
        var serverName = "test-server";
        var existingConfig = new MCPServerConfig
        {
            ServerName = serverName,
            Command = "python"
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            [serverName] = existingConfig
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);
        _mockMcpExtensionWrapper.Setup(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteAsync(serverName);

        // Assert
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockGAgentFactory.Object, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentServer_ShouldThrowException()
    {
        // Arrange
        var serverName = "non-existent-server";
        var mockConfigs = new Dictionary<string, MCPServerConfig>();

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.DeleteAsync(serverName));
        Assert.Contains($"MCP server '{serverName}' not found", exception.Message);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task DeleteAsync_WithInvalidServerName_ShouldThrowException(string serverName)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.DeleteAsync(serverName));
        Assert.Contains("Server name is required", exception.Message);
    }

    #endregion

    #region GetServerNamesAsync Tests

    [Fact]
    public async Task GetServerNamesAsync_ShouldReturnAllServerNames()
    {
        // Arrange
        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server1"] = new MCPServerConfig { ServerName = "server1" },
            ["server2"] = new MCPServerConfig { ServerName = "server2" },
            ["server3"] = new MCPServerConfig { ServerName = "server3" }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetServerNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("server1", result);
        Assert.Contains("server2", result);
        Assert.Contains("server3", result);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    [Fact]
    public async Task GetServerNamesAsync_WithEmptyConfig_ShouldReturnEmptyList()
    {
        // Arrange
        var mockConfigs = new Dictionary<string, MCPServerConfig>();

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetServerNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    #endregion

    #region GetRawConfigurationsAsync Tests

    [Fact]
    public async Task GetRawConfigurationsAsync_ShouldReturnRawConfigurations()
    {
        // Arrange
        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server1"] = new MCPServerConfig 
            { 
                ServerName = "server1",
                Command = "python",
                Description = "Server 1"
            },
            ["server2"] = new MCPServerConfig 
            { 
                ServerName = "server2",
                Command = "node",
                Description = "Server 2"
            }
        };

        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object))
            .ReturnsAsync(mockConfigs);

        // Act
        var result = await _controller.GetRawConfigurationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("server1"));
        Assert.True(result.ContainsKey("server2"));
        Assert.Equal("python", result["server1"].Command);
        Assert.Equal("node", result["server2"].Command);
        
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockGAgentFactory.Object), Times.Once);
    }

    #endregion
}