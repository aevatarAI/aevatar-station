namespace Aevatar.GAgents.MCP.Core.Model;

[GenerateSerializer]
public class MCPToolResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public object? Data { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public Dictionary<string, string> Metadata { get; set; } = new();
}
