using System;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.AIHttpAsyncWoker;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.AITextToImageWorker;

public class AITextToImageWorker : GrainAsyncWorker<AITextToImageRequest, AITextToImageResponse>
{
    private readonly IBrainFactory _brainFactory;

    public AITextToImageWorker(ILogger<GrainAsyncWorker<AITextToImageRequest, AITextToImageResponse>> logger,
        LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    protected override async Task<AITextToImageResponse> PerformLongRunTask(
        IGrainAsyncHandler<AITextToImageResponse> grainAsyncHandler, AITextToImageRequest request)
    {
        var result = new AITextToImageResponse();
        result.Context = request.Context;
        result.TextToImageOption = request.TextToImageOption;

        try
        {
            result = await AITextToImageHandlerAsync(request);
            result.Context = request.Context;
            result.TextToImageOption = request.TextToImageOption;
            return result;
        }
        catch (Exception ex)
        {
            var exception = AIException.ConvertAndRethrowException(ex);
            result.ErrorEnum = exception.ExceptionEnum;
            result.ErrorMessage = ex.Message;
            Logger.LogError($"[AITextToImageWorker][AITextToImageHandler] handle error:{exception.ToString()}");
        }

        return result;
    }

    private async Task<AITextToImageResponse> AITextToImageHandlerAsync(AITextToImageRequest request)
    {
        if (request.Context != null)
        {
            Logger.LogDebug(
                $"[AITextToImageWorker][AITextToImageHandler] chatRequest start:{request.Context.Context}");
        }

        var brain = _brainFactory.GetTextToImageBrain(request.LlmConfig);
        if (brain == null)
        {
            return new AITextToImageResponse()
            {
                ErrorEnum = AIExceptionEnum.ArgumentNullError,
                ErrorMessage =
                    $"AITextToImageHandler Can not found Brain, llmconfig:{JsonConvert.SerializeObject(request.LlmConfig)}"
            };
        }

        await brain.InitializeAsync(request.LlmConfig, string.Empty, string.Empty);
        if (request.Context != null)
        {
            Logger.LogDebug(
                $"[AITextToImageWorker][AITextToImageHandler] request init brain:{request.Context.Context}");
        }

        var response = await brain.GenerateTextToImageAsync(request.Prompt, request.TextToImageOption);
        if (response == null)
        {
            Logger.LogError("[AITextToImageWorker][AITextToImageHandler] response == null");
            return new AITextToImageResponse()
            {
                ErrorEnum = AIExceptionEnum.ArgumentNullError,
                ErrorMessage = $"[AITextToImageWorker][AITextToImageHandler] response == null"
            };
        }

        if (request.Context != null)
        {
            Logger.LogDebug(
                $"[AITextToImageWorker][AITextToImageHandler] text to image,context:{request.Context.Context}");
        }

        var result = new AITextToImageResponse();
        result.Context = request.Context;
        result.ImageResponses = response;

        return result;
    }
}