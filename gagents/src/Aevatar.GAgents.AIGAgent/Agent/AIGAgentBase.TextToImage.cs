using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.AITextToImageWorker;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent, IGrainAsyncHandler<AITextToImageResponse>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    protected async Task<List<TextToImageResponse>?> GenerateImageAsync(string prompt,
        TextToImageOption? textToImageOption = null, CancellationToken cancellationToken = default)
    {
        if (_brain == null)
        {
            Logger.LogDebug($"[AIGAgentBase][TextToImageAsync] _brain==null");
            return null;
        }

        var text2ImageBrain = ConvertBrain<ITextToImageBrain>();

        List<TextToImageResponse>? response = null;
        try
        {
            textToImageOption = textToImageOption ?? new TextToImageOption();
            response = await text2ImageBrain.GenerateTextToImageAsync(prompt, textToImageOption, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[AIGAgentBase][TextToImageAsync] exception error:{ex.ToString()}");
            throw AIException.ConvertAndRethrowException(ex);
        }

        return response;
    }

    protected async Task<bool> TextToImageAsync(string prompt, TextToImageContextDto context,
        TextToImageOption? textToImageOption = null, bool ifAsync = true)
    {
        var request = new AITextToImageRequest()
        {
            Prompt = prompt,
            TextToImageOption = textToImageOption ?? new TextToImageOption(),
            LlmConfig = State.LLM,
            Context = context,
        };

        return await CreateLongRunTaskAsync<AITextToImageRequest, AITextToImageResponse>(request, ifAsync);
    }

    public async Task HandleStreamAsync(AITextToImageResponse arg)
    {
        await AITextToImageHandleAsync(arg.Context, arg.ErrorEnum, arg.ErrorMessage, arg.ImageResponses);
    }

    protected virtual Task AITextToImageHandleAsync(TextToImageContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage, List<TextToImageResponse>? imageResponses)
    {
        return Task.CompletedTask;
    }
}