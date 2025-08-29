
using System;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public class AIArgumentNullException : AIException
{
    public AIArgumentNullException(string message, Exception ex) : base(message,ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.ArgumentNullError;
}
