using System;
using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.GEvents;

[Description("Return a streaming chunk")]
[GenerateSerializer]
public class AIStreamingResponseGEvent : EventBase
{
    [Id(0)] public string ResponseContent { get; set; }
    [Id(1)] public int SerialNumber { get; set; }
    [Id(2)] public AIChatContextDto Context { get; set; } = new();
    [Id(3)] public bool IsLastChunk { get; set; }
    
    [Id(4)] public string ChatId { get; set; }
    
    [Id(5)] public Guid SessionId { get; set; }
    
    [Id(6)] public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    
    [Id(7)] public string Response { get; set; }


}

[Description("Return a error reponse")]
[GenerateSerializer]
public class AIStreamingErrorResponseGEvent : EventBase
{
    [Id(0)] public AIChatContextDto Context { get; set; } = new();
    
    [Id(1)]
    public GrainId GrainId { get; set; }

    [Id(2)]
    public Type HandleExceptionType { get; set; }

    [Id(3)]
    public string ExceptionMessage { get; set; }
}

[GenerateSerializer]
public enum ResponseType
{
    ChatResponse = 2,
}