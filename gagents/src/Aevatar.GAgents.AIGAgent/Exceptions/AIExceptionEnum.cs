using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public enum AIExceptionEnum
{
    None = 0,
    ArgumentError = 1001,
    ArgumentNullError = 1002,
    HttpOperationError = 1003,
    RequestLimitError = 1004,
    ClientResultError =1005,
    OtherException = 1999,
}