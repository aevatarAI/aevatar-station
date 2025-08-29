namespace Aevatar.GAgents.MCP.Core.Model;

[GenerateSerializer]
public class MCPToolInfo
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, MCPParameterInfo> Parameters { get; set; } = new();
    [Id(3)] public string ServerName { get; set; } = string.Empty;
}
