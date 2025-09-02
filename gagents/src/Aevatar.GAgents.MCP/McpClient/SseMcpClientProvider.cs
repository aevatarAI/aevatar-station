using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;
using System.Text;

namespace Aevatar.GAgents.MCP.McpClient;

public class SseMcpClientProvider : McpClientProviderBase
{
    public override McpClientType ClientType => McpClientType.Sse;

    protected override IClientTransport CreateClientTransport(MCPServerConfig config)
    {
        var headers = new Dictionary<string, string>(config.Headers);

        // Configure authentication headers based on OAuth configuration
        if (config.OAuth != null)
        {
            AddAuthenticationHeaders(headers, config.OAuth);
        }

        var options = new SseClientTransportOptions
        {
            Name = config.ServerName,
            Endpoint = new Uri(config.Url!),
            AdditionalHeaders = headers
        };

        return new SseClientTransport(options);
    }

    /// <summary>
    /// Add authentication headers based on OAuth configuration
    /// </summary>
    private void AddAuthenticationHeaders(Dictionary<string, string> headers, MCPOAuthConfig oauthConfig)
    {
        switch (oauthConfig.ProviderType.ToLowerInvariant())
        {
            case "bearer" when !string.IsNullOrWhiteSpace(oauthConfig.AccessToken):
                headers["Authorization"] = $"Bearer {oauthConfig.AccessToken}";
                break;
            
            case "basic" when oauthConfig.AdditionalParameters.ContainsKey("username") && 
                             oauthConfig.AdditionalParameters.ContainsKey("password"):
                var username = oauthConfig.AdditionalParameters["username"];
                var password = oauthConfig.AdditionalParameters["password"];
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                headers["Authorization"] = $"Basic {credentials}";
                break;
            
            case "oauth2" when !string.IsNullOrWhiteSpace(oauthConfig.AccessToken):
                // For OAuth2, if we have an access token, use it as Bearer
                headers["Authorization"] = $"Bearer {oauthConfig.AccessToken}";
                
                // Add any additional OAuth2 headers
                foreach (var param in oauthConfig.AdditionalParameters)
                {
                    if (param.Key.StartsWith("header_", StringComparison.OrdinalIgnoreCase))
                    {
                        var headerName = param.Key.Substring(7); // Remove "header_" prefix
                        headers[headerName] = param.Value;
                    }
                }
                break;
            
            case "custom":
                // For custom authentication, add all additional parameters as headers
                foreach (var param in oauthConfig.AdditionalParameters)
                {
                    headers[param.Key] = param.Value;
                }
                break;
        }
    }
}