using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.GEvents;

namespace Aevatar.GAgents.MCP.GEvents;

[GenerateSerializer]
[Description("Call a tool on MCP server")]
public class MCPToolCallEvent : EventWithResponseBase<MCPToolResponseEvent>
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public string ToolName { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object> Arguments { get; set; } = new();
    [Id(3)] public Guid RequestId { get; set; } = Guid.NewGuid();
}