using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.AIHttpAsyncWoker;

public class AIHttpAsyncWorker : GrainAsyncWorker<AIHttpAsyncRequest, AIHttpAsyncResponse>
{
    private readonly IBrainFactory _brainFactory;

    public AIHttpAsyncWorker(ILogger<GrainAsyncWorker<AIHttpAsyncRequest, AIHttpAsyncResponse>> logger,
        LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    protected override async Task<AIHttpAsyncResponse> PerformLongRunTask(
        IGrainAsyncHandler<AIHttpAsyncResponse> grainAsyncHandler, AIHttpAsyncRequest request)
    {
        AIHttpAsyncResponse result = new AIHttpAsyncResponse();
        result.Context = request.Context;
        try
        {
            result = await AIHttpRequestAsync(grainAsyncHandler, request);
        }
        catch (Exception ex)
        {
            var exception = AIException.ConvertAndRethrowException(ex);
            result.ErrorEnum = exception.ExceptionEnum;
            result.ErrorMessage = ex.Message;
            Logger.LogError($"[BaseLongStreamWorker][PerformLongRunTask] handle error:{exception.ToString()}");
        }
        
        return result;
    }

    private async Task<AIHttpAsyncResponse> AIHttpRequestAsync(
        IGrainAsyncHandler<AIHttpAsyncResponse> grainAsyncHandler, AIHttpAsyncRequest chatRequest)
    {
        if (chatRequest.Context != null)
        {
            Logger.LogDebug(
                $"[AIHttpAsyncWorker][AIHttpRequestAsync] chatRequest start:{chatRequest.Context.RequestId}-{chatRequest.Context.ChatId}-{chatRequest.Context.MessageId}");
        }

        var streamingConfig = chatRequest.StreamingConfig;

        var cancellationToken = new CancellationToken();
        if (streamingConfig?.TimeOutInternal > 0)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(streamingConfig.TimeOutInternal));
            cancellationToken = cts.Token;
        }

        var brain = _brainFactory.GetChatBrain(chatRequest.LlmConfig);
        if (brain == null)
        {
            return new AIHttpAsyncResponse()
            {
                ErrorEnum = AIExceptionEnum.ArgumentNullError,
                ErrorMessage = $"Can not found Brain, llmconfig:{JsonConvert.SerializeObject(chatRequest.LlmConfig)}"
            };
        }

        await brain.InitializeAsync(chatRequest.LlmConfig, chatRequest.VectorId, chatRequest.Instructions);
        if (chatRequest.Context != null)
        {
            Logger.LogDebug(
                $"[AIHttpAsyncWorker][AIHttpRequestAsync] chatRequest init brain:{chatRequest.Context.RequestId}-{chatRequest.Context.ChatId}-{chatRequest.Context.MessageId}");
        }

        var response = await brain.InvokePromptAsync(chatRequest.Content, chatRequest.ImageKeys, chatRequest.History,
            chatRequest.IfUseKnowledge,
            chatRequest.PromptSettings,
            cancellationToken: cancellationToken);
        if (response == null)
        {
            Logger.LogError("[AIHttpAsyncWorker][AIHttpRequestAsync] response == null");
            return new AIHttpAsyncResponse()
            {
                ErrorEnum = AIExceptionEnum.ArgumentNullError,
                ErrorMessage = $"[AIHttpAsyncWorker][AIHttpRequestAsync] response == null"
            };
        }
        
        if (chatRequest.Context != null)
        {
            Logger.LogDebug(
                $"[AIHttpAsyncWorker][AIHttpRequestAsync] llm response:{chatRequest.Context.RequestId}-{chatRequest.Context.ChatId}-{chatRequest.Context.MessageId}");
        }
        
        var result = new AIHttpAsyncResponse();
        result.Context = chatRequest.Context;
        result.TokenUsageStatistics = response.TokenUsageStatistics;
        result.ResponseContent = response.ChatReponseList.Count > 0 ? response.ChatReponseList[0].Content : "";

        return result;
    }
}