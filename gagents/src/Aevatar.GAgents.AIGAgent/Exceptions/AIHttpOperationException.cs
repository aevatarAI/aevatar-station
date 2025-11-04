using System;
using System.Net;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public class AIHttpOperationException : AIException
{
    [Id(0)] public HttpStatusCode? State { get; }
    [Id(1)] public string? ResponseContent { get; }

    public AIHttpOperationException(HttpStatusCode? state, string? responseContent, string message, Exception ex) :
        base(message, ex)
    {
        State = state;
        ResponseContent = responseContent;
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.HttpOperationError;
}