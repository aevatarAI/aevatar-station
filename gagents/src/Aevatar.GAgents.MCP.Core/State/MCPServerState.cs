using Orleans;

namespace Aevatar.GAgents.MCP.Core.State;

[GenerateSerializer]
public class MCPServerState
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public bool IsConnected { get; set; }
    [Id(2)] public DateTime LastConnectedTime { get; set; }
    [Id(3)] public string SessionId { get; set; } = string.Empty;
    [Id(4)] public List<string> RegisteredTools { get; set; } = new();
    [Id(5)] public string Type { get; set; } = string.Empty;
    [Id(6)] public int ToolCount { get; set; }
}
