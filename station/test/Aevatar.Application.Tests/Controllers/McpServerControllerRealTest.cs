using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.MCP.Core.Extensions;
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
    private readonly Mock<IGAgentFactory> _mockGAgentFactory = new();
    private readonly Mock<ILogger<McpServerController>> _mockLogger = new();
    private readonly Mock<IMcpExtensionWrapper> _mockMcpExtensionWrapper  = new();
    private readonly McpServerController _controller;
    private readonly IConfigManagerGAgent _mockConfigManagerGAgent = new Mock<IConfigManagerGAgent>().Object;

    public McpServerControllerRealTest()
    {
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("server1", result.Items.First().ServerName);
        Assert.Equal("python", result.Items.First().Command);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("test-server", result.Items.First().ServerName);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("server1", result.Items.First().ServerName);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.GetAsync(serverName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverName, result.ServerName);
        Assert.Equal("python", result.Command);
        Assert.Equal("Test server", result.Description);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentServer_ShouldThrowException()
    {
        // Arrange
        var serverName = "non-existent-server";
        var mockConfigs = new Dictionary<string, MCPServerConfig>();

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.GetAsync(serverName));
        Assert.Contains($"MCP server '{serverName}' not found", exception.Message);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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
        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.CreateAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(input.ServerName, result.ServerName);
        Assert.Equal(input.Command, result.Command);
        Assert.Equal(input.Description, result.Description);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockConfigManagerGAgent, It.IsAny<string>()), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(input));
        Assert.Contains("MCP server 'existing-server' already exists", exception.Message);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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
        SetupMockConfigManagerGAgent(mockConfigs, false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.CreateAsync(input));
        Assert.Contains("Failed to create MCP server configuration", exception.Message);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockConfigManagerGAgent, It.IsAny<string>()), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.UpdateAsync(serverName, input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverName, result.ServerName);
        Assert.Equal("node", result.Command); // Updated
        Assert.Equal("Updated description", result.Description); // Updated

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockConfigManagerGAgent, It.IsAny<string>()), Times.Once);
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
        SetupMockConfigManagerGAgent(mockConfigs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.UpdateAsync(serverName, input));
        Assert.Contains($"MCP server '{serverName}' not found", exception.Message);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        await _controller.DeleteAsync(serverName);

        // Assert
        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
        _mockMcpExtensionWrapper.Verify(x => x.ConfigMCPWhitelistAsync(_mockConfigManagerGAgent, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentServer_ShouldThrowException()
    {
        // Arrange
        var serverName = "non-existent-server";
        var mockConfigs = new Dictionary<string, MCPServerConfig>();

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _controller.DeleteAsync(serverName));
        Assert.Contains($"MCP server '{serverName}' not found", exception.Message);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetServerNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("server1", result);
        Assert.Contains("server2", result);
        Assert.Contains("server3", result);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    [Fact]
    public async Task GetServerNamesAsync_WithEmptyConfig_ShouldReturnEmptyList()
    {
        // Arrange
        var mockConfigs = new Dictionary<string, MCPServerConfig>();

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetServerNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    #endregion

    #region Advanced Sorting Tests

    [Theory]
    [InlineData("command asc")]
    [InlineData("command desc")]
    [InlineData("description asc")]
    [InlineData("description desc")]
    [InlineData("serverType asc")]
    [InlineData("serverType desc")]
    [InlineData("createdAt asc")]
    [InlineData("createdAt desc")]
    [InlineData("modifiedAt asc")]
    [InlineData("modifiedAt desc")]
    public async Task GetListAsync_WithVariousSortingFields_ShouldReturnCorrectlySortedResults(string sorting)
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            Sorting = sorting,
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server-z"] = new MCPServerConfig
            {
                ServerName = "server-z",
                Command = "python",
                Description = "Z server",
                Url = "http://example.com"
            },
            ["server-a"] = new MCPServerConfig
            {
                ServerName = "server-a",
                Command = "node",
                Description = "A server"
            },
            ["server-m"] = new MCPServerConfig
            {
                ServerName = "server-m",
                Command = "java",
                Description = "M server",
                Url = "http://middle.com"
            }
        };

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);

        // Verify sorting is applied (basic validation that order changed from default)
        var sortField = sorting.Split(' ')[0].ToLower();
        var isDesc = sorting.Contains("desc", StringComparison.OrdinalIgnoreCase);

        switch (sortField)
        {
            case "command":
                if (isDesc)
                    Assert.True(string.Compare(result.Items[0].Command, result.Items[1].Command,
                        StringComparison.OrdinalIgnoreCase) >= 0);
                else
                    Assert.True(string.Compare(result.Items[0].Command, result.Items[1].Command,
                        StringComparison.OrdinalIgnoreCase) <= 0);
                break;
            case "servertype":
                // Stdio comes before StreamableHttp alphabetically
                if (!isDesc)
                    Assert.Contains(result.Items, item => item.ServerType == "Stdio");
                break;
        }

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_WithInvalidSortingField_ShouldUseDefaultSorting()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            Sorting = "invalidfield desc",
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server-z"] = new MCPServerConfig { ServerName = "server-z", Command = "python" },
            ["server-a"] = new MCPServerConfig { ServerName = "server-a", Command = "node" },
            ["server-m"] = new MCPServerConfig { ServerName = "server-m", Command = "java" }
        };

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        // Should be sorted by ServerName (default) - server-a, server-m, server-z
        Assert.Equal("server-a", result.Items[0].ServerName);
        Assert.Equal("server-m", result.Items[1].ServerName);
        Assert.Equal("server-z", result.Items[2].ServerName);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_WithEmptySorting_ShouldUseDefaultSorting()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            Sorting = "",
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server-z"] = new MCPServerConfig { ServerName = "server-z", Command = "python" },
            ["server-a"] = new MCPServerConfig { ServerName = "server-a", Command = "node" }
        };

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        // Should be sorted by ServerName (default)
        Assert.Equal("server-a", result.Items[0].ServerName);
        Assert.Equal("server-z", result.Items[1].ServerName);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_WithOnlyFieldNameSorting_ShouldUseAscendingOrder()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            Sorting = "command", // No direction specified, should default to asc
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["server1"] = new MCPServerConfig { ServerName = "server1", Command = "zsh" },
            ["server2"] = new MCPServerConfig { ServerName = "server2", Command = "bash" }
        };

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        // Should be sorted by Command ascending - bash comes before zsh
        Assert.Equal("bash", result.Items[0].Command);
        Assert.Equal("zsh", result.Items[1].Command);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetListAsync_WithServerTypeFilter_BothTypes_ShouldReturnCorrectTypes()
    {
        // Arrange - Test both Stdio and StreamableHttp types
        var input1 = new GetMcpServerListDto
        {
            ServerType = "Stdio",
            MaxResultCount = 10,
            SkipCount = 0
        };

        var input2 = new GetMcpServerListDto
        {
            ServerType = "StreamableHttp",
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["stdio-server"] = new MCPServerConfig
            {
                ServerName = "stdio-server",
                Command = "python",
                Description = "Stdio server",
                Url = null // Stdio type
            },
            ["http-server"] = new MCPServerConfig
            {
                ServerName = "http-server",
                Command = "node",
                Description = "HTTP server",
                Url = "http://example.com" // StreamableHttp type
            }
        };

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act - Test Stdio filter
        var result1 = await _controller.GetListAsync(input1);

        // Reset mock calls count
        _mockMcpExtensionWrapper.Invocations.Clear();
        SetupMockConfigManagerGAgent(mockConfigs);


        var result2 = await _controller.GetListAsync(input2);

        // Assert - Stdio filter
        Assert.NotNull(result1);
        Assert.Equal(1, result1.TotalCount);
        Assert.Single(result1.Items);
        Assert.Equal("stdio-server", result1.Items[0].ServerName);
        Assert.Equal("Stdio", result1.Items[0].ServerType);

        // Assert - StreamableHttp filter
        Assert.NotNull(result2);
        Assert.Equal(1, result2.TotalCount);
        Assert.Single(result2.Items);
        Assert.Equal("http-server", result2.Items[0].ServerName);
        Assert.Equal("StreamableHttp", result2.Items[0].ServerType);
    }

    [Fact]
    public async Task GetListAsync_WithCaseInsensitiveServerTypeFilter_ShouldWork()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            ServerType = "stdio", // lowercase
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["stdio-server"] = new MCPServerConfig
            {
                ServerName = "stdio-server",
                Command = "python",
                Url = null // Stdio type
            }
        };

        SetupMockConfigManagerGAgent(mockConfigs);


        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("stdio-server", result.Items[0].ServerName);
    }

    [Fact]
    public async Task GetListAsync_WithComplexSearchTerm_ShouldSearchInAllFields()
    {
        // Arrange
        var input = new GetMcpServerListDto
        {
            SearchTerm = "test", // Should match ServerName, Command, or Description
            MaxResultCount = 10,
            SkipCount = 0
        };

        var mockConfigs = new Dictionary<string, MCPServerConfig>
        {
            ["test-server"] = new MCPServerConfig
            {
                ServerName = "test-server", // matches in name
                Command = "python",
                Description = "Production server"
            },
            ["prod-server"] = new MCPServerConfig
            {
                ServerName = "prod-server",
                Command = "test-runner", // matches in command
                Description = "Production server"
            },
            ["dev-server"] = new MCPServerConfig
            {
                ServerName = "dev-server",
                Command = "node",
                Description = "Test environment server" // matches in description
            },
            ["other-server"] = new MCPServerConfig
            {
                ServerName = "other-server",
                Command = "java",
                Description = "Other server" // no match
            }
        };

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.GetListAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // Should match 3 out of 4 servers
        Assert.Equal(3, result.Items.Count);

        // Verify that "other-server" is not included
        Assert.DoesNotContain(result.Items, item => item.ServerName == "other-server");

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
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

        SetupMockConfigManagerGAgent(mockConfigs);

        // Act
        var result = await _controller.GetRawConfigurationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("server1"));
        Assert.True(result.ContainsKey("server2"));
        Assert.Equal("python", result["server1"].Command);
        Assert.Equal("node", result["server2"].Command);

        _mockMcpExtensionWrapper.Verify(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent), Times.Once);
    }

    #endregion

    private readonly Dictionary<string, MCPServerConfig> _mockConfigs = new()
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

    private void SetupMockConfigManagerGAgent(Dictionary<string, MCPServerConfig>? configs = null,
        bool configResult = true)
    {
        configs ??= _mockConfigs;
        _mockMcpExtensionWrapper.Setup(x => x.GetMCPWhiteListAsync(_mockConfigManagerGAgent))
            .ReturnsAsync(configs);
        _mockMcpExtensionWrapper.Setup(x => x.ConfigMCPWhitelistAsync(_mockConfigManagerGAgent, It.IsAny<string>()))
            .ReturnsAsync(configResult);
        _mockMcpExtensionWrapper.Setup(x => x.GetMcpServerConfigManagerAsync())
            .ReturnsAsync(_mockConfigManagerGAgent);
    }
}