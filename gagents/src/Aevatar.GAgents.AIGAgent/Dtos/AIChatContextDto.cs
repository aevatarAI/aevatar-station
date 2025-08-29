using System;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class AIChatContextDto
{
    [Id(0)] public Guid RequestId { get; set; }
    [Id(2)] public string MessageId { get; set; }
    [Id(3)] public string ChatId { get; set; }
}