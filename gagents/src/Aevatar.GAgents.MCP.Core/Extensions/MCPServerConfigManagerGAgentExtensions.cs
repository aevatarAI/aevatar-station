using System.Text.Json;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Core.Extensions;

// ReSharper disable InconsistentNaming
public static class MCPServerConfigManagerGAgentExtensions
{
    public static readonly string MCPWhitelistConfigTypeFullName = typeof(MCPServerOptions).FullName!;
    public static Guid MCPWhitelistConfigGuid = MCPWhitelistConfigTypeFullName.ToGuid();

    public static async Task<bool> ConfigMCPWhitelistAsync(this IConfigManagerGAgent configManagerGAgent,
        Dictionary<string, MCPServerConfig> mcpServersConfig)
    {
        if (mcpServersConfig.IsNullOrEmpty())
        {
            return false;
        }

        return await configManagerGAgent.ConfigMCPWhitelistAsync(JsonSerializer.Serialize(mcpServersConfig));
    }

    public static async Task<bool> ConfigMCPWhitelistAsync(this IConfigManagerGAgent configManagerGAgent,
        string configJson)
    {
        if (string.IsNullOrEmpty(configJson)) return false;
        JsonDocument.Parse(configJson);
        await configManagerGAgent.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = MCPWhitelistConfigTypeFullName,
            ConfigJson = configJson
        });

        return true;
    }

    public static async Task<Dictionary<string, MCPServerConfig>> GetMCPWhiteListAsync(
        this IConfigManagerGAgent configManagerGAgent)
    {
        try
        {
            var requestEvent = new ConfigRequestEvent
            {
                ConfigType = MCPWhitelistConfigTypeFullName!
            };

            var response = await configManagerGAgent.RequestConfigAsync(requestEvent);

            if (!response.Success || string.IsNullOrEmpty(response.ConfigJson))
            {
                return [];
            }

            var whitelist = JsonSerializer.Deserialize<Dictionary<string, MCPServerConfig>>(response.ConfigJson);
            return whitelist ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static async Task<MCPWhitelistValidationResult> ValidateServerAgainstWhitelistAsync(
        this IConfigManagerGAgent configManagerGAgent, List<MCPServerConfig> servers)
    {
        var whitelist = await configManagerGAgent.GetMCPWhiteListAsync();
        foreach (var server in servers)
        {
            // Check if server exists in whitelist
            if (!whitelist.TryGetValue(server.ServerName, out var whitelistServer))
            {
                return new MCPWhitelistValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"MCP server '{server.ServerName}' is not in the whitelist"
                };
            }

            // For SSE (StreamableHttp) transport, allow without strict validation
            if (server.Type == MCPServerType.StreamableHttp || !server.Url.IsNullOrEmpty())
            {
                continue;
            }

            var validationResult = ValidateStdioServerConfiguration(server, whitelistServer);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }
        }

        return new MCPWhitelistValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validate Stdio MCP server configuration with strict matching
    /// </summary>
    /// <param name="server">MCP server configuration to validate</param>
    /// <param name="whitelistServer">Whitelisted MCP server configuration</param>
    /// <returns>Validation result</returns>
    private static MCPWhitelistValidationResult ValidateStdioServerConfiguration(
        MCPServerConfig server,
        MCPServerConfig whitelistServer)
    {
        // Check server name (should already match from dictionary lookup, but double-check)
        if (!string.Equals(server.ServerName, whitelistServer.ServerName, StringComparison.OrdinalIgnoreCase))
        {
            return new MCPWhitelistValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Server name mismatch: '{server.ServerName}' != '{whitelistServer.ServerName}'"
            };
        }

        // Check command
        if (!string.Equals(server.Command, whitelistServer.Command, StringComparison.Ordinal))
        {
            return new MCPWhitelistValidationResult
            {
                IsValid = false,
                ErrorMessage =
                    $"Command mismatch for server '{server.ServerName}': '{server.Command}' != '{whitelistServer.Command}'"
            };
        }

        // Check args (must be exactly the same)
        if (!ArgsMatch(server.Args, whitelistServer.Args))
        {
            return new MCPWhitelistValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Arguments mismatch for server '{server.ServerName}': " +
                               $"[{string.Join(", ", server.Args)}] != [{string.Join(", ", whitelistServer.Args)}]"
            };
        }

        return new MCPWhitelistValidationResult { IsValid = true };
    }

    /// <summary>
    /// Check if two argument lists match exactly
    /// </summary>
    /// <param name="args1">First argument list</param>
    /// <param name="args2">Second argument list</param>
    /// <returns>True if arguments match exactly</returns>
    private static bool ArgsMatch(List<string> args1, List<string> args2)
    {
        if (args1.Count != args2.Count)
            return false;

        return !args1.Where((t, i) => !string.Equals(t, args2[i], StringComparison.Ordinal)).Any();
    }
}