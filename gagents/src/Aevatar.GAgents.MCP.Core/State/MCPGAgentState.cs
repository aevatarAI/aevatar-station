using Aevatar.GAgents.MCP.Options;
using GroupChat.GAgent.GEvent;
using Orleans;

namespace Aevatar.GAgents.MCP.Core.State;

[GenerateSerializer]
public class MCPGAgentState : MemberState
{
    [Id(0)] public MCPServerConfig MCPServerConfig { get; set; }
    [Id(1)] public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    [Id(2)] public DateTime LastToolCall { get; set; }
    
    /// <summary>
    /// Session ID for gateway connections
    /// </summary>
    [Id(3)] public string? SessionId { get; set; }
    
    /// <summary>
    /// Gateway adapter name for routing
    /// </summary>
    [Id(4)] public string? GatewayAdapterName { get; set; }
    
    /// <summary>
    /// Connection type being used (Direct, Gateway)
    /// </summary>
    [Id(5)] public string ConnectionType { get; set; } = "Direct";
    
    /// <summary>
    /// Last successful connection timestamp
    /// </summary>
    [Id(6)] public DateTime? LastConnected { get; set; }
    
    /// <summary>
    /// Connection retry count
    /// </summary>
    [Id(7)] public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Last connection error message
    /// </summary>
    [Id(8)] public string? LastConnectionError { get; set; }
}