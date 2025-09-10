using System.Collections.Generic;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.MCP.Options;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentConfigDto : GroupMemberConfigDto
{
    [Id(0)] 
    public string Instructions { get; set; } = "You are a helpful AI assistant";

    [Id(1)] public string SystemLLM { get; set; } = "OpenAI";

    [Id(2)] public List<MCPServerConfig> MCPServers { get; set; } = [];
    [Id(3)] public List<GrainType> ToolGAgentTypes { get; set; } = [];
    [Id(4)] public List<GrainId> ToolGAgents { get; set; } = [];
}