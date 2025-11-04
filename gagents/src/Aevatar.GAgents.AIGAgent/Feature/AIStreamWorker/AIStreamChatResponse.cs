using Aevatar.AI.Exceptions;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.StreamSyncWoker;

[GenerateSerializer]
public class AIStreamChatResponseEvent : EventBase
{
    [Id(0)] public AIChatContextDto Context { get; set; }
    [Id(1)] public TokenUsageStatistics? TokenUsageStatistics { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public AIExceptionEnum ErrorEnum { get; set; } =  AIExceptionEnum.None;
    [Id(4)] public AIStreamChatContent? ChatContent { get; set; }
}

[GenerateSerializer]
public class AIStreamChatContent
{
    [Id(0)] public string ResponseContent { get; set; }
    [Id(1)] public int SerialNumber { get; set; }
    [Id(2)] public bool IsLastChunk { get; set; }
    [Id(3)] public bool IsAggregationMsg { get; set; }
    [Id(4)] public string AggregationMsg { get; set; }
}