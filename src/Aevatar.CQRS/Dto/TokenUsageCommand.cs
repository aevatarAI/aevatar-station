using System;
using System.Collections.Generic;
using Aevatar.GAgents.AI.Options;
using MediatR;

namespace Aevatar.CQRS.Dto;


public class TokenUsageCommand : IRequest
{
    public List<TokenUsage> TokenUsages { get; set; }
}

public class TokenUsage
{
    public string GrainId { get; set; }
    public string SystemLLMConfig { get; set; }
    public bool IfUserLLMProvider { get; set; }
    public int InputTokenUsage { get; set; } = 0;
    public int OutTokenUsage { get; set; } = 0;
    public int TotalTokenUsage { get; set; } = 0;
    public int LastInputTokenUsage { get; set; } = 0;
    public int LastOutTokenUsage { get; set; } = 0;
    public int LastTotalTokenUsage { get; set; } = 0;
    public DateTime CreatTime { get; set; } = DateTime.Now;
}