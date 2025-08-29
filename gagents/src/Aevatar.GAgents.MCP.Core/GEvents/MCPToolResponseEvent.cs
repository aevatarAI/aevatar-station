using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.MCP.Core.GEvents;

[GenerateSerializer]
[Description("Response from MCP tool call")]
public class MCPToolResponseEvent : EventBase
{
    [Id(0)] public Guid RequestId { get; set; }
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public object? Result { get; set; }
    [Id(3)] public string? ErrorMessage { get; set; }
    [Id(4)] public string ServerName { get; set; } = string.Empty;
    [Id(5)] public string ToolName { get; set; } = string.Empty;
}
