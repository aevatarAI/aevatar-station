using System;
using System.Collections.Generic;
using Aevatar.GAgents.MCP.Core.Options;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.MCP.Test.Options;

/// <summary>
/// Unit tests for MCPGatewayConfig
/// </summary>
public class MCPGatewayConfigTests
{
    [Fact]
    public void IsValid_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com",
            AuthToken = "valid-token-12345",
            RequestTimeout = TimeSpan.FromSeconds(30),
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1)
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void IsValid_WithEmptyGatewayBaseUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "",
            AuthToken = "valid-token-12345"
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Gateway base URL is required");
    }

    [Fact]
    public void IsValid_WithInvalidUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "invalid-url",
            AuthToken = "valid-token-12345"
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Gateway base URL must be a valid HTTP/HTTPS URL");
    }

    [Fact]
    public void IsValid_WithEmptyAuthToken_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com",
            AuthToken = ""
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Auth token is required");
    }

    [Fact]
    public void IsValid_WithShortAuthToken_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com",
            AuthToken = "short"
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Auth token must be between 10 and 1000 characters");
    }

    [Fact]
    public void IsValid_WithInvalidRequestTimeout_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com",
            AuthToken = "valid-token-12345",
            RequestTimeout = TimeSpan.FromMilliseconds(500)
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Request timeout must be between 1 second and 10 minutes");
    }

    [Fact]
    public void IsValid_WithInvalidMaxRetryAttempts_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com",
            AuthToken = "valid-token-12345",
            MaxRetryAttempts = -1
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Max retry attempts must be between 0 and 10");
    }

    [Fact]
    public void IsValid_WithInvalidRetryDelay_ShouldReturnFalse()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com",
            AuthToken = "valid-token-12345",
            RetryDelay = TimeSpan.FromMilliseconds(50)
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.ShouldBeFalse();
        errors.ShouldContain("Retry delay must be between 100ms and 1 minute");
    }

    [Fact]
    public void GetAdapterUrl_WithValidInputs_ShouldReturnCorrectUrl()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com"
        };

        // Act
        var url = config.GetAdapterUrl("test-adapter");

        // Assert
        url.ShouldBe("https://gateway.example.com/adapters/test-adapter/mcp");
    }

    [Fact]
    public void GetAdapterUrl_WithCustomEndpoint_ShouldReturnCorrectUrl()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = "https://gateway.example.com/"
        };

        // Act
        var url = config.GetAdapterUrl("test-adapter", "sse");

        // Assert
        url.ShouldBe("https://gateway.example.com/adapters/test-adapter/sse");
    }

    [Fact]
    public void GetAuthenticatedHeaders_WithoutSessionId_ShouldReturnBasicHeaders()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            AuthToken = "test-token",
            DefaultHeaders = new Dictionary<string, string>
            {
                ["Custom-Header"] = "custom-value"
            }
        };

        // Act
        var headers = config.GetAuthenticatedHeaders();

        // Assert
        headers.ShouldContainKeyAndValue("Authorization", "Bearer test-token");
        headers.ShouldContainKeyAndValue("User-Agent", "Aevatar-MCP-Gateway-Client/1.0");
        headers.ShouldContainKeyAndValue("Accept", "application/json");
        headers.ShouldContainKeyAndValue("Custom-Header", "custom-value");
        headers.ShouldNotContainKey("X-Session-ID");
    }

    [Fact]
    public void GetAuthenticatedHeaders_WithSessionId_ShouldIncludeSessionHeader()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            AuthToken = "Bearer test-token"
        };

        // Act
        var headers = config.GetAuthenticatedHeaders("session-123");

        // Assert
        headers.ShouldContainKeyAndValue("Authorization", "Bearer test-token");
        headers.ShouldContainKeyAndValue("X-Session-ID", "session-123");
    }

    [Fact]
    public void GetAuthenticatedHeaders_WithBearerToken_ShouldNotAddBearerPrefix()
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            AuthToken = "Bearer existing-bearer-token"
        };

        // Act
        var headers = config.GetAuthenticatedHeaders();

        // Assert
        headers.ShouldContainKeyAndValue("Authorization", "Bearer existing-bearer-token");
    }

    [Theory]
    [InlineData("https://gateway.com", "test-adapter", "mcp", "https://gateway.com/adapters/test-adapter/mcp")]
    [InlineData("https://gateway.com/", "my-adapter", "sse", "https://gateway.com/adapters/my-adapter/sse")]
    [InlineData("http://localhost:8080", "local-adapter", "health",
        "http://localhost:8080/adapters/local-adapter/health")]
    public void GetAdapterUrl_WithVariousInputs_ShouldReturnCorrectUrls(string baseUrl, string adapterName,
        string endpoint, string expectedUrl)
    {
        // Arrange
        var config = new MCPGatewayConfig
        {
            GatewayBaseUrl = baseUrl
        };

        // Act
        var url = config.GetAdapterUrl(adapterName, endpoint);

        // Assert
        url.ShouldBe(expectedUrl);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new MCPGatewayConfig();

        // Assert
        config.GatewayBaseUrl.ShouldBe(string.Empty);
        config.AuthToken.ShouldBe(string.Empty);
        config.RequestTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        config.EnableSessionAffinity.ShouldBeTrue();
        config.DefaultHeaders.ShouldNotBeNull();
        config.DefaultHeaders.ShouldBeEmpty();
        config.MaxRetryAttempts.ShouldBe(3);
        config.RetryDelay.ShouldBe(TimeSpan.FromSeconds(1));
        config.EnableDetailedLogging.ShouldBeFalse();
        config.HealthCheckPath.ShouldBe("/health");
    }
}