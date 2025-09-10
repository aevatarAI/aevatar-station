using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.MCP.Options;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    [Id(0)] public string Instructions { get; set; }

    [Required] [Id(1)] public LLMConfigDto LLMConfig { get; set; }
    [Id(2)] public bool StreamingModeEnabled { get; set; }
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }
    [Id(5)] public List<GrainType> AllowedGAgentTypes { get; set; } = [];

    // MCP configuration options
    [Id(7)] public List<MCPServerConfig> MCPServers { get; set; } = [];
    [Id(8)] public List<GrainType> ToolGAgentTypes { get; set; } = [];
    [Id(9)] public List<GrainId> ToolGAgents { get; set; } = [];
}