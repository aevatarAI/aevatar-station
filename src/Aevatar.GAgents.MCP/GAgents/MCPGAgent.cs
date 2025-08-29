using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.State;
using Aevatar.GAgents.MCP.Options;
using GroupChat.GAgent.Feature.Common;

namespace Aevatar.GAgents.MCP.GAgents;

/// <summary>
/// MCP GAgent implementation using official SDK
/// </summary>
[GenerateSerializer]
[GAgent(AevatarGAgentsConstants.MCPGAgentAlias, "aevatar")]
public class MCPGAgent : MCPGAgentBase<MCPGAgentState, MCPGAgentStateLogEvent, EventBase, MCPGAgentConfig>,
    IMCPGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("MCP GAgent for interacting with Model Context Protocol servers");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(1);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        return Task.FromResult(new ChatResponse
        {
            Skip = true,
            Continue = false
        });
    }
}

/// <summary>
/// State log event for MCPGAgent
/// </summary>
[GenerateSerializer]
public class MCPGAgentStateLogEvent : StateLogEventBase<MCPGAgentStateLogEvent>;