using System;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public class AIArgumentException : AIException
{
    public AIArgumentException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.ArgumentError;
}