using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class MultiAIChatConfig : ConfigurationBase
{
    [Id(0)]
    public string Instructions { get; set; } = "You are a helpful AI assistant with access to multiple language models";
    
    [Id(1)]
    public int MaxHistoryCount { get; set; } = 20;
    
    [Id(2)]
    public bool StreamingModeEnabled { get; set; } = true;
    
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }
    
    [Id(4)] public List<LLMConfigDto> LLMConfigs { get; set; }
    
    [Id(5)] public TimeSpan RequestRecoveryDelay { get; set; } = TimeSpan.FromMinutes(1);
    
    [Id(6)]
    public string PrimaryModel { get; set; } = "gpt-4";
    
    [Id(7)]
    public string FallbackModel { get; set; } = "gpt-3.5-turbo";
    
    [Id(8)]
    public int MaxRetries { get; set; } = 3;
    
    [Id(9)]
    public bool EnableLoadBalancing { get; set; } = true;
}