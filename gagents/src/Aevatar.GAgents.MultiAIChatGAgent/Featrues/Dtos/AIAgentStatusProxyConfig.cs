using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class AIAgentStatusProxyConfig : ConfigurationBase
{
    [Id(0)]
    public string Instructions { get; set; } = "You are an AI agent status monitor responsible for tracking and reporting agent health";
    
    [Id(1)] public LLMConfigDto LLMConfig { get; set; }
    
    [Id(3)]
    public bool StreamingModeEnabled { get; set; } = false;
    
    [Id(4)] public StreamingConfig StreamingConfig { get; set; }
    
    [Id(5)] public TimeSpan? RequestRecoveryDelay { get; set; }
    
    [Id(6)] public Guid ParentId { get; set; }
    
    [Id(7)]
    public int CheckInterval { get; set; } = 30;
    
    [Id(8)]
    public int MaxStatusAge { get; set; } = 300;
    
    [Id(9)]
    public bool EnableHealthCheck { get; set; } = true;
}