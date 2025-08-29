using System;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public class AIOtherException : AIException
{
    public AIOtherException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.OtherException;
}