using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public LLMConfig? LLM { get; set; }
    [Id(1)] public string? SystemLLM { get; set; } = null;
    [Id(2)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(3)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(4)] public int InputTokenUsage { get; set; } = 0;
    [Id(5)] public int OutTokenUsage { get; set; } = 0;
    [Id(6)] public int TotalTokenUsage { get; set; } = 0;
    [Id(7)] public bool StreamingModeEnabled { get; set; }
    [Id(8)] public StreamingConfig StreamingConfig { get; set; }
    [Id(9)] public int LastInputTokenUsage { get; set; } = 0;
    [Id(10)] public int LastOutTokenUsage { get; set; } = 0;
    [Id(11)] public int LastTotalTokenUsage { get; set; } = 0;
    [Id(12)] public string? LLMConfigKey { get; set; } = null;

    // GAgentTool-related state fields
    [Id(13)] public bool EnableGAgentTools { get; set; } = false;
    [Id(14)] public List<string> RegisteredGAgentFunctions { get; set; } = [];
    [Id(16)] public List<GrainId> ToolGAgents { get; set; } = [];

    // MCP-related state fields
    [Id(17)] public bool EnableMCPTools { get; set; } = false;
    [Id(18)] public Dictionary<string, MCPGAgentReference> MCPAgents { get; set; } = new();

    [Id(19)] public List<ToolCallHistoryEntry> ToolCallHistory { get; set; } = new();
}

[GenerateSerializer]
public class MCPGAgentReference
{
    [Id(0)] public Guid AgentId { get; set; }
    [Id(1)] public string ServerName { get; set; } = string.Empty;
    [Id(2)] public string Description { get; set; } = string.Empty;
}