using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.Model;

namespace Aevatar.GAgents.MCP.GEvents;

[GenerateSerializer]
public class MCPToolsDiscoveredEvent : EventBase
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public List<MCPToolInfo> Tools { get; set; } = new();
}