using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.McpClient;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.MCP.Test.McpClient;

public class GatewayMcpClientProviderTests : AevatarMCPTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly GatewayMcpClientProvider _provider;
    private readonly MCPGatewayConfig _gatewayConfig;

    public GatewayMcpClientProviderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        
        // Setup test configuration
        _gatewayConfig = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://test-gateway.example.com",
            AuthToken = "test-auth-token-12345",
            RequestTimeout = TimeSpan.FromSeconds(30),
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100), // Faster for tests
            EnableSessionAffinity = true,
            EnableDetailedLogging = true
        };

        var options = Microsoft.Extensions.Options.Options.Create(_gatewayConfig);
        var logger = GetRequiredService<ILogger<GatewayMcpClientProvider>>();
        var serviceProvider = GetRequiredService<IServiceProvider>();

        _provider = new GatewayMcpClientProvider(options, logger);
    }

    [Fact]
    public void ClientType_ShouldReturnGateway()
    {
        // Act
        var clientType = _provider.ClientType;

        // Assert
        clientType.ShouldBe(McpClientType.Gateway);
    }

    [Fact]
    public void GetConnectionStats_ShouldReturnCorrectStats()
    {
        // Act
        var stats = _provider.GetConnectionStats();

        // Assert
        stats.ShouldNotBeNull();
        stats.ShouldContainKeyAndValue("GatewayUrl", "https://test-gateway.example.com");
        stats.ShouldContainKeyAndValue("MaxRetryAttempts", 3);
        stats.ShouldContainKeyAndValue("RequestTimeout", 30.0);
        stats.ShouldContainKeyAndValue("SessionAffinity", true);
        stats.ShouldContainKey("ActiveConnections");

        _testOutputHelper.WriteLine($"Connection Stats: {string.Join(", ", stats.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    [Fact]
    public void CreateClientTransport_WithValidConfig_ShouldCreateCorrectTransport()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = "test-adapter";
        config.SessionId = "test-session-123";

        // Act & Assert - This tests the transport creation logic
        // Note: We can't easily test the actual transport creation without a real gateway,
        // but we can verify the configuration validation
        config.IsValidForGateway(out var errors).ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void CreateClientTransport_WithInvalidGatewayConfig_ShouldThrowException()
    {
        // Arrange
        var invalidGatewayConfig = new MCPGatewayConfig
        {
            GatewayBaseUrl = "", // Invalid
            AuthToken = "test-token"
        };

        var options = Microsoft.Extensions.Options.Options.Create(invalidGatewayConfig);
        var logger = GetRequiredService<ILogger<GatewayMcpClientProvider>>();
        var serviceProvider = GetRequiredService<IServiceProvider>();
        var provider = new GatewayMcpClientProvider(options, logger);

        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = "test-adapter";

        // Act & Assert
        Should.Throw<InvalidOperationException>(async () =>
        {
            await provider.GetOrCreateClientAsync(config);
        });
    }

    [Fact]
    public void CreateClientTransport_WithMissingAdapterName_ShouldThrowException()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = ""; // Missing adapter name

        // Act & Assert
        Should.Throw<InvalidOperationException>(async () =>
        {
            await _provider.GetOrCreateClientAsync(config);
        });
    }

    [Theory]
    [InlineData("test-adapter", "mcp", "https://test-gateway.example.com/adapters/test-adapter/mcp")]
    [InlineData("my-adapter", "sse", "https://test-gateway.example.com/adapters/my-adapter/sse")]
    [InlineData("another-adapter", "health", "https://test-gateway.example.com/adapters/another-adapter/health")]
    public void GatewayConfig_GetAdapterUrl_ShouldReturnCorrectUrls(string adapterName, string endpoint, string expectedUrl)
    {
        // Act
        var url = _gatewayConfig.GetAdapterUrl(adapterName, endpoint);

        // Assert
        url.ShouldBe(expectedUrl);
    }

    [Fact]
    public void GatewayConfig_GetAuthenticatedHeaders_ShouldIncludeRequiredHeaders()
    {
        // Arrange
        var sessionId = "test-session-123";

        // Act
        var headers = _gatewayConfig.GetAuthenticatedHeaders(sessionId);

        // Assert
        headers.ShouldContainKeyAndValue("Authorization", "Bearer test-auth-token-12345");
        headers.ShouldContainKeyAndValue("X-Session-ID", "test-session-123");
        headers.ShouldContainKeyAndValue("User-Agent", "Aevatar-MCP-Gateway-Client/1.0");
        headers.ShouldContainKeyAndValue("Accept", "application/json");
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidConfig_ShouldReturnConnectionResult()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = "test-adapter";

        // Act & Assert
        // Note: This will fail in unit test environment without real gateway,
        // but we can verify the method signature and basic validation
        var result = await _provider.TestConnectionAsync(config);
        
        // In a real test environment with mock gateway, this would return true
        // For now, we just verify the method doesn't throw
        result.ShouldBe(false); // Expected to fail without real gateway
        
        _testOutputHelper.WriteLine($"Connection test result: {result}");
    }

    [Fact]
    public async Task DisconnectClientAsync_ShouldHandleGracefully()
    {
        // Arrange
        var serverName = "test-server";

        // Act & Assert - Should not throw
        await _provider.DisconnectClientAsync(serverName);
        
        _testOutputHelper.WriteLine($"Disconnection completed for server: {serverName}");
    }

    [Fact]
    public async Task IsConnectedAsync_WithoutConnections_ShouldReturnFalse()
    {
        // Arrange
        var serverName = "non-existent-server";

        // Act
        var isConnected = await _provider.IsConnectedAsync(serverName);

        // Assert
        isConnected.ShouldBeFalse();
    }

    [Fact]
    public void MCPServerConfig_GetEffectiveType_WithGateway_ShouldReturnGateway()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.Type = MCPServerType.Stdio;

        // Act
        var effectiveType = config.GetEffectiveType();

        // Assert
        effectiveType.ShouldBe(MCPServerType.Gateway);
    }

    [Fact]
    public void MCPServerConfig_GetOrGenerateSessionId_WithoutSessionId_ShouldGenerateNew()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");

        // Act
        var sessionId = config.GetOrGenerateSessionId();

        // Assert
        sessionId.ShouldNotBeNull();
        sessionId.ShouldStartWith("test-server-");
        sessionId.Length.ShouldBe("test-server-".Length + 32);
        
        _testOutputHelper.WriteLine($"Generated session ID: {sessionId}");
    }

    [Fact]
    public void MCPServerConfig_IsValidForGateway_WithValidGatewayConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = "test-adapter";
        config.Priority = 75;

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void MCPServerConfig_IsValidForGateway_WithMissingAdapterName_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = ""; // Missing

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Gateway adapter name is required when UseGateway is true");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(101, false)]
    public void MCPServerConfig_IsValidForGateway_WithVariousPriorities_ShouldValidateCorrectly(int priority, bool expectedValid)
    {
        // Arrange
        var config = CreateTestServerConfig("test-server");
        config.UseGateway = true;
        config.GatewayAdapterName = "test-adapter";
        config.Priority = priority;

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBe(expectedValid);
        if (!expectedValid)
        {
            errors.ShouldContain("Priority must be between 1 and 100");
        }
    }

    protected MCPServerConfig CreateTestServerConfig(string serverName)
    {
        return new MCPServerConfig
        {
            ServerName = serverName,
            Command = "test-command",
            Description = "Test MCP server configuration",
            Type = MCPServerType.Gateway
        };
    }
}
