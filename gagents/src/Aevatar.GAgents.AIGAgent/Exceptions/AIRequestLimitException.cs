using System;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public class AIRequestLimitException : AIException
{
    public AIRequestLimitException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.RequestLimitError;
}