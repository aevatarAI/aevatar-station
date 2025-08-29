using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.AIHttpAsyncWoker;

[GenerateSerializer]
public class AIHttpAsyncResponse
{
    [Id(0)] public AIChatContextDto Context { get; set; }
    [Id(1)] public TokenUsageStatistics? TokenUsageStatistics { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public AIExceptionEnum ErrorEnum { get; set; } =  AIExceptionEnum.None;
    [Id(4)] public string? ResponseContent { get; set; }
}