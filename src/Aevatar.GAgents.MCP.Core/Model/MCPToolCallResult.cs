using Orleans;

namespace Aevatar.GAgents.MCP.Core.Model;

/// <summary>
/// MCP tool call result
/// </summary>
[GenerateSerializer]
public class MCPToolCallResult
{
    /// <summary>
    /// Whether the call was successful
    /// </summary>
    [Id(0)]
    public bool Success { get; set; }

    /// <summary>
    /// Return data
    /// </summary>
    [Id(1)]
    public string? Data { get; set; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    [Id(2)]
    public string? ErrorMessage { get; set; }
}