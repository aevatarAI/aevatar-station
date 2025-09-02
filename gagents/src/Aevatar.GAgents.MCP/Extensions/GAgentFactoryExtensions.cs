using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Extensions;

public static class GAgentFactoryExtensions
{
    public static async Task<IMCPGAgent> GetFilesystemMCPGAgent(this IGAgentFactory gAgentFactory,
        params string[] paths)
    {
        if (DefaultMCPServers.Configs.TryGetValue(DefaultMCPServers.FilesystemMCPServerName, out var config))
        {
            if (!paths.IsNullOrEmpty())
            {
                config.Args = ["-y", "@modelcontextprotocol/server-filesystem"];
                config.Args.AddRange(paths);
            }

            return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
            {
                MemberName = DefaultMCPServers.FilesystemMCPServerName,
                ServerConfig = config
            });
        }

        throw new MCPServerConfigNotFoundException("MCP Server config of filesystem not found.");
    }

    public static async Task<IMCPGAgent> GetMCPGAgentAsync(IGAgentFactory gAgentFactory, DefaultMCPServer mcpServer,
        Dictionary<string, string>? env = null)
    {
        var mcpServerName = mcpServer.ToMCPServerName();
        if (DefaultMCPServers.Configs.TryGetValue(mcpServerName, out var config))
        {
            if ((int)mcpServer > 1000)
            {
                if (env == null)
                {
                    throw new MCPServerConfigNotFoundException(
                        $"Env of mcp server config of {mcpServerName} not found.");
                }

                config.Env = env;
            }

            return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
            {
                MemberName = mcpServerName,
                ServerConfig = config
            });
        }

        throw new MCPServerConfigNotFoundException($"MCP Server config of {mcpServerName} not found.");
    }
}