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
}