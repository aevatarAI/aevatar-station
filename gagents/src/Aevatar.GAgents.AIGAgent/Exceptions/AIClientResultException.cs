using System;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public class AIClientResultException : AIException
{
    public AIClientResultException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.ClientResultError;
}