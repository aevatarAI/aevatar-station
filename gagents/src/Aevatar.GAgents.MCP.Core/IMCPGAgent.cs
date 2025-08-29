using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Core.State;

namespace Aevatar.GAgents.MCP.Core;

public interface IMCPGAgent : IStateGAgent<MCPGAgentState>
{
    Task<List<MCPToolInfo>> GetAvailableToolsAsync(string? serverName = null);
    Task<MCPToolResponseEvent> CallToolAsync(string serverName, string toolName, Dictionary<string, object> arguments);
}
