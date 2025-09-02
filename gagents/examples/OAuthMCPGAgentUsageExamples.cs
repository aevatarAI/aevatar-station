using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Extensions;
using Aevatar.GAgents.MCP.Options;

namespace Examples;

/// <summary>
/// Examples demonstrating OAuth-enabled MCP server usage
/// </summary>
public class OAuthMCPGAgentUsageExamples
{
    private readonly IGAgentFactory _gAgentFactory;

    public OAuthMCPGAgentUsageExamples(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    /// <summary>
    /// Example 1: Bearer Token Authentication
    /// </summary>
    public async Task<IMCPGAgent> CreateBearerTokenMCPGAgentAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "bearer",
            AccessToken = "your-bearer-token-here"
        };

        return await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "api-server",
            url: "https://api.example.com/mcp/events",
            oauthConfig: oauthConfig,
            description: "API server with Bearer authentication"
        );
    }

    /// <summary>
    /// Example 2: OAuth2 with Client Credentials
    /// </summary>
    public async Task<IMCPGAgent> CreateOAuth2MCPGAgentAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "oauth2",
            ClientId = "your-client-id",
            ClientSecret = "your-client-secret",
            AuthorizationUrl = "https://auth.example.com/oauth/authorize",
            TokenUrl = "https://auth.example.com/oauth/token",
            Scopes = new List<string> { "read", "write" },
            AdditionalParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["audience"] = "mcp-api"
            }
        };

        return await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "oauth2-server",
            url: "https://secure.api.com/mcp/stream",
            oauthConfig: oauthConfig,
            additionalHeaders: new Dictionary<string, string>
            {
                ["X-Client-Version"] = "1.0",
                ["Accept"] = "text/event-stream"
            },
            description: "OAuth2 authenticated MCP server"
        );
    }

    /// <summary>
    /// Example 3: OAuth2 with Refresh Token
    /// </summary>
    public async Task<IMCPGAgent> CreateOAuth2WithRefreshTokenMCPGAgentAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "oauth2",
            ClientId = "your-client-id",
            ClientSecret = "your-client-secret",
            AccessToken = "current-access-token",
            RefreshToken = "refresh-token",
            TokenUrl = "https://auth.example.com/oauth/token",
            Scopes = new List<string> { "repo", "read:user" }
        };

        return await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "github-like-server",
            url: "https://api.github-like.com/mcp/events",
            oauthConfig: oauthConfig,
            description: "GitHub-like server with OAuth2 and refresh token"
        );
    }

    /// <summary>
    /// Example 4: Basic Authentication
    /// </summary>
    public async Task<IMCPGAgent> CreateBasicAuthMCPGAgentAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "basic",
            AdditionalParameters = new Dictionary<string, string>
            {
                ["username"] = "your-username",
                ["password"] = "your-password"
            }
        };

        return await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "internal-server",
            url: "https://internal.company.com/mcp/stream",
            oauthConfig: oauthConfig,
            description: "Internal server with Basic authentication"
        );
    }

    /// <summary>
    /// Example 5: Using server from configuration with OAuth
    /// </summary>
    public async Task<IMCPGAgent?> CreateFromConfigurationAsync()
    {
        // This assumes the server is configured in appsettings.json with OAuth
        return await _gAgentFactory.GetMCPGAgentAsync("github-oauth");
    }

    /// <summary>
    /// Example 6: Programmatic configuration with environment variables
    /// </summary>
    public async Task<IMCPGAgent> CreateWithEnvironmentVariablesAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "oauth2",
            ClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? throw new InvalidOperationException("GITHUB_CLIENT_ID not set"),
            ClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET") ?? throw new InvalidOperationException("GITHUB_CLIENT_SECRET not set"),
            AuthorizationUrl = "https://github.com/login/oauth/authorize",
            TokenUrl = "https://github.com/login/oauth/access_token",
            Scopes = new List<string> { "repo", "read:user", "read:org" }
        };

        return await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "github-dynamic",
            url: "https://api.github.com/mcp/stream",
            oauthConfig: oauthConfig,
            additionalHeaders: new Dictionary<string, string>
            {
                ["User-Agent"] = "MyApp/1.0",
                ["Accept"] = "text/event-stream"
            },
            description: "GitHub MCP server with dynamic OAuth configuration"
        );
    }

    /// <summary>
    /// Example 7: Custom Authentication Headers
    /// </summary>
    public async Task<IMCPGAgent> CreateCustomAuthMCPGAgentAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "custom",
            AdditionalParameters = new Dictionary<string, string>
            {
                ["X-API-Key"] = "your-api-key",
                ["X-Client-ID"] = "your-client-id",
                ["X-Signature"] = "computed-signature"
            }
        };

        return await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "custom-auth-server",
            url: "https://custom.api.com/mcp/stream",
            oauthConfig: oauthConfig,
            description: "Server with custom authentication headers"
        );
    }

    /// <summary>
    /// Example 8: Testing OAuth configuration
    /// </summary>
    public async Task TestOAuthConfigurationAsync()
    {
        var oauthConfig = new MCPOAuthConfig
        {
            ProviderType = "bearer",
            AccessToken = "test-token"
        };

        // Validate configuration before creating the agent
        var config = new MCPServerConfig
        {
            ServerName = "test-server",
            Url = "https://test.api.com/mcp/events",
            Type = MCPServerType.StreamableHttp,
            OAuth = oauthConfig,
            Headers = new Dictionary<string, string>()
        };

        var validationResult = config.Validate();
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Invalid configuration: {validationResult.GetErrorSummary()}");
        }

        var mcpAgent = await _gAgentFactory.GetStreamableHttpMCPGAgentWithOAuthAsync(
            serverName: "test-server",
            url: "https://test.api.com/mcp/events",
            oauthConfig: oauthConfig,
            description: "Test server for OAuth validation"
        );

        // Test the connection
        var tools = await mcpAgent.GetAvailableToolsAsync();
        Console.WriteLine($"Successfully connected to OAuth-enabled MCP server. Available tools: {tools.Count}");
    }
}
