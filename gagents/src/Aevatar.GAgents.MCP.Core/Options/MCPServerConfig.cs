namespace Aevatar.GAgents.MCP.Options;

// ReSharper disable InconsistentNaming
[GenerateSerializer]
public class MCPServerConfig
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public string Command { get; set; } = string.Empty;
    [Id(2)] public List<string> Args { get; set; } = [];
    [Id(3)] public Dictionary<string, string> Env { get; set; } = new();
    [Id(4)] public string Description { get; set; } = string.Empty;
    [Id(5)] public string? Url { get; set; }
    [Id(6)] public MCPServerType Type { get; set; }
}

/// <summary>
/// https://modelcontextprotocol.io/specification/2025-06-18/basic/transports
/// </summary>
[GenerateSerializer]
public enum MCPServerType
{
    Stdio,
    StreamableHttp
}

/// <summary>
/// Tool definition for predefined tools in configuration
/// </summary>
[GenerateSerializer]
public class MCPToolDefinition
{
    [Id(0)]
    public string Name { get; set; } = string.Empty;
    
    [Id(1)]
    public string Description { get; set; } = string.Empty;
    
    [Id(2)]
    public Dictionary<string, MCPParameterDefinition>? Parameters { get; set; }
}

/// <summary>
/// Parameter definition for predefined tools
/// </summary>
[GenerateSerializer]
public class MCPParameterDefinition
{
    [Id(0)]
    public string Type { get; set; } = "string";
    
    [Id(1)]
    public string? Description { get; set; }
    
    [Id(2)]
    public bool Required { get; set; }
}

public static class MCPServerConfigExtensions
{
    public static bool IsValid(this MCPServerConfig config)
    {
        return !string.IsNullOrWhiteSpace(config.ServerName);
    }
}