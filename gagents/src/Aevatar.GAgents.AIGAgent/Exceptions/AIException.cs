using System;
using System.ClientModel;
using System.Net;
using Azure;
using Microsoft.SemanticKernel;
using Orleans;

namespace Aevatar.AI.Exceptions;

[GenerateSerializer]
public abstract class AIException : Exception
{
    public abstract AIExceptionEnum ExceptionEnum { get; }

    public AIException(string message, Exception ex) : base(message, ex)
    {
    }

    public static AIException ConvertAndRethrowException(Exception ex)
    {
        switch (ex)
        {
            case ClientResultException clientResultException:
                if (clientResultException.Status == (int)HttpStatusCode.TooManyRequests)
                {
                    return new AIRequestLimitException(clientResultException.Message, ex);
                }
                
                return new AIClientResultException(clientResultException.Message, ex);
            case ArgumentNullException argumentNullException:
                return new AIArgumentNullException(argumentNullException.Message, ex);
            case ArgumentException argumentException:
                return new AIArgumentException(argumentException.Message, ex);
            case HttpOperationException httpOperationException:
                if (httpOperationException.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return new AIRequestLimitException(httpOperationException.Message, ex);
                }

                return new AIHttpOperationException(httpOperationException.StatusCode,
                    httpOperationException.ResponseContent, httpOperationException.Message, ex);
            case RequestFailedException requestFailedException:
                if (requestFailedException.Status == (int)HttpStatusCode.TooManyRequests)
                {
                    return new AIRequestLimitException(requestFailedException.Message, ex);
                }

                return new AIHttpOperationException(
                    requestFailedException.Status is HttpStatusCode
                        ? (HttpStatusCode)requestFailedException.Status
                        : (HttpStatusCode)0,
                    null, requestFailedException.Message, ex);
            default:
                return new AIOtherException(ex.Message, ex);
        }
    }
}