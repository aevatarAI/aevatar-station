using Aevatar.GAgents.MCP.Options;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.MCP.Test.Options;

/// <summary>
/// Unit tests for MCPServerConfigExtensions
/// </summary>
public class MCPServerConfigExtensionsTests
{
    [Fact]
    public void IsValidForGateway_WithValidGatewayConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = true,
            GatewayAdapterName = "test-adapter",
            Priority = 50
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void IsValidForGateway_WithMissingGatewayAdapterName_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = true,
            GatewayAdapterName = ""
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Gateway adapter name is required when UseGateway is true");
    }

    [Fact]
    public void IsValidForGateway_WithLongGatewayAdapterName_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = true,
            GatewayAdapterName = new string('a', 101) // 101 characters
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Gateway adapter name must not exceed 100 characters");
    }

    [Fact]
    public void IsValidForGateway_WithLongSessionId_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = true,
            GatewayAdapterName = "test-adapter",
            SessionId = new string('s', 201) // 201 characters
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Session ID must not exceed 200 characters");
    }

    [Fact]
    public void IsValidForGateway_WithInvalidPriority_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = true,
            GatewayAdapterName = "test-adapter",
            Priority = 0 // Invalid priority
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Priority must be between 1 and 100");
    }

    [Fact]
    public void IsValidForGateway_WithDirectConnectionAndMissingCommandAndUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = false,
            Command = "",
            Url = ""
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Either Command or URL is required for direct connections");
    }

    [Fact]
    public void IsValidForGateway_WithDirectConnectionAndCommand_ShouldReturnTrue()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = false,
            Command = "node server.js"
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void IsValidForGateway_WithDirectConnectionAndUrl_ShouldReturnTrue()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = false,
            Url = "http://localhost:3000"
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetEffectiveType_WithGatewayEnabled_ShouldReturnGateway()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            UseGateway = true,
            Type = MCPServerType.Stdio
        };

        // Act
        var effectiveType = config.GetEffectiveType();

        // Assert
        effectiveType.ShouldBe(MCPServerType.Gateway);
    }

    [Fact]
    public void GetEffectiveType_WithGatewayDisabled_ShouldReturnConfiguredType()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            UseGateway = false,
            Type = MCPServerType.StreamableHttp
        };

        // Act
        var effectiveType = config.GetEffectiveType();

        // Assert
        effectiveType.ShouldBe(MCPServerType.StreamableHttp);
    }

    [Fact]
    public void GetOrGenerateSessionId_WithExistingSessionId_ShouldReturnExisting()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            SessionId = "existing-session-123"
        };

        // Act
        var sessionId = config.GetOrGenerateSessionId();

        // Assert
        sessionId.ShouldBe("existing-session-123");
    }

    [Fact]
    public void GetOrGenerateSessionId_WithoutSessionId_ShouldGenerateNew()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            SessionId = null
        };

        // Act
        var sessionId = config.GetOrGenerateSessionId();

        // Assert
        sessionId.ShouldNotBeNull();
        sessionId.ShouldStartWith("test-server-");
        sessionId.Length.ShouldBe("test-server-".Length + 32); // server name + hyphen + 32-char GUID
    }

    [Fact]
    public void GetOrGenerateSessionId_WithEmptySessionId_ShouldGenerateNew()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            SessionId = ""
        };

        // Act
        var sessionId = config.GetOrGenerateSessionId();

        // Assert
        sessionId.ShouldNotBeNull();
        sessionId.ShouldStartWith("test-server-");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(101, false)]
    [InlineData(-1, false)]
    public void IsValidForGateway_WithVariousPriorityValues_ShouldValidateCorrectly(int priority, bool expectedValid)
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            UseGateway = true,
            GatewayAdapterName = "test-adapter",
            Priority = priority
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBe(expectedValid);
        if (!expectedValid)
        {
            errors.ShouldContain("Priority must be between 1 and 100");
        }
    }

    [Fact]
    public void IsValidForGateway_WithInvalidServerName_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "", // Invalid server name
            UseGateway = true,
            GatewayAdapterName = "test-adapter"
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Basic server configuration is invalid");
    }

    [Fact]
    public void IsValidForGateway_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var config = new MCPServerConfig
        {
            ServerName = "", // Invalid
            UseGateway = true,
            GatewayAdapterName = "", // Invalid
            Priority = 0, // Invalid
            SessionId = new string('s', 201) // Invalid
        };

        // Act
        var isValid = config.IsValidForGateway(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.Count.ShouldBeGreaterThan(1);
        errors.ShouldContain("Basic server configuration is invalid");
        errors.ShouldContain("Gateway adapter name is required when UseGateway is true");
        errors.ShouldContain("Priority must be between 1 and 100");
        errors.ShouldContain("Session ID must not exceed 200 characters");
    }
}