using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.State;
using Aevatar.GAgents.MCP.Options;
using GroupChat.GAgent.Feature.Common;

[module: Interceptor]

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

    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] { WorkflowIdProperty, RoundIdProperty, GrainIdProperty })]
    protected override Task<int> GetInterestValueAsync()
    {
        return Task.FromResult(1);
    }

    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] { WorkflowIdProperty, RoundIdProperty, GrainIdProperty })]
    protected override Task<ChatResponse> ChatAsync(List<ChatMessage>? coordinatorMessages)
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