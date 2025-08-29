using System.Collections.Generic;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.StreamSyncWoker;


[GenerateSerializer]
public class AIStreamChatRequest
{
    [Id(0)] public LLMConfig LlmConfig { get; set; }
    [Id(1)] public string Instructions { get; set; }
    [Id(2)] public string VectorId { get; set; }
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }
    [Id(4)] public string Content { get; set; }
    [Id(5)] public List<ChatMessage>? History { get; set; }
    [Id(6)] public bool IfUseKnowledge { get; set; } = false;
    [Id(7)] public ExecutionPromptSettings? PromptSettings { get; set; } = null;
    [Id(8)] public AIChatContextDto Context { get; set; } = null;
    [Id(9)] public List<string>? ImageKeys { get; set; }
}