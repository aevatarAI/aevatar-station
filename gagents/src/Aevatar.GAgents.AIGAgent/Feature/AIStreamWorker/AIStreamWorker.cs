using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Orleans;
using Orleans.Concurrency;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public class AIStreamWorker : GrainAsyncWorker<AIStreamChatRequest, AIStreamChatResponseEvent>
{
    private readonly IBrainFactory _brainFactory;

    public AIStreamWorker(ILogger<GrainAsyncWorker<AIStreamChatRequest, AIStreamChatResponseEvent>> logger,
        LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    protected override async Task<AIStreamChatResponseEvent> PerformLongRunTask(
        IGrainAsyncHandler<AIStreamChatResponseEvent> grainAsyncHandler, AIStreamChatRequest chatRequest)
    {
        AIStreamChatResponseEvent result = new AIStreamChatResponseEvent();
        result.Context = chatRequest.Context;

        try
        {
            result = await AIStreamRequestAsync(grainAsyncHandler, chatRequest);
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

    private async Task<AIStreamChatResponseEvent> AIStreamRequestAsync(
        IGrainAsyncHandler<AIStreamChatResponseEvent> grainAsyncHandler, AIStreamChatRequest chatRequest)
    {
        if (chatRequest.Context != null)
        {
            Logger.LogDebug(
                $"[AIStreamRequestAsync] chatRequest start:{chatRequest.Context.RequestId}-{chatRequest.Context.ChatId}-{chatRequest.Context.MessageId}");
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
            return new AIStreamChatResponseEvent()
            {
                ErrorEnum = AIExceptionEnum.ArgumentNullError,
                ErrorMessage = $"Can not found Brain, llmconfig:{JsonConvert.SerializeObject(chatRequest.LlmConfig)}"
            };
        }

        await brain.InitializeAsync(chatRequest.LlmConfig, chatRequest.VectorId, chatRequest.Instructions);
        if (chatRequest.Context != null)
        {
            Logger.LogDebug(
                $"[AIStreamRequestAsync] chatRequest init brain:{chatRequest.Context.RequestId}-{chatRequest.Context.ChatId}-{chatRequest.Context.MessageId}");
        }

        var responseStreaming = await brain.InvokePromptStreamingAsync(chatRequest.Content, chatRequest.ImageKeys, chatRequest.History,
            chatRequest.IfUseKnowledge,
            chatRequest.PromptSettings,
            cancellationToken: cancellationToken);

        var result =
            await HandleAIResponseAsync(chatRequest, grainAsyncHandler, responseStreaming, streamingConfig, brain);

        return result;
    }

    private async Task<AIStreamChatResponseEvent> HandleAIResponseAsync(AIStreamChatRequest chatRequest,
        IGrainAsyncHandler<AIStreamChatResponseEvent> grainAsyncHandler, IAsyncEnumerable<object> responseStreaming,
        StreamingConfig? streamingConfig, IChatBrain brain)
    {
        var chatMessage = new ChatMessage();
        var streamingMessageContentList = new List<object>();
        var bufferingSize = streamingConfig?.BufferingSize ?? 0;
        var stringBuilder = new StringBuilder();
        var completeContent = new StringBuilder();
        var chunkNumber = 0;

        await foreach (var messageContent in responseStreaming)
        {
            if (messageContent is not StreamingChatMessageContent streamingChatMessageContent) continue;

            streamingMessageContentList.Add(streamingChatMessageContent);
            stringBuilder.Append(streamingChatMessageContent.Content);
            if (stringBuilder.Length < bufferingSize) continue;

            if (chatRequest.Context != null && chunkNumber == 0)
            {
                Logger.LogDebug(
                    $"[AIStreamRequestAsync] chatRequest first response:{chatRequest.Context.RequestId}-{chatRequest.Context.ChatId}-{chatRequest.Context.MessageId}");
            }

            var chunk = bufferingSize == 0
                ? stringBuilder.ToString()
                : stringBuilder.ToString(0, bufferingSize);
            var response = new AIStreamChatResponseEvent();
            response.Context = chatRequest.Context;
            response.ChatContent = new AIStreamChatContent()
            {
                SerialNumber = chunkNumber++,
                ResponseContent = chunk
            };
            await grainAsyncHandler.HandleStreamAsync(response);

            completeContent.Append(chunk);
            if (bufferingSize == 0)
            {
                stringBuilder.Clear();
            }
            else
            {
                stringBuilder.Remove(0, bufferingSize);
            }

            if (streamingChatMessageContent.Role.HasValue)
            {
                chatMessage.ChatRole = ConvertToChatRole(streamingChatMessageContent.Role.Value);
            }
        }

        completeContent.Append(stringBuilder.ToString());
        var result = new AIStreamChatResponseEvent();
        result.Context = chatRequest.Context;
        result.TokenUsageStatistics = brain.GetStreamingTokenUsage(streamingMessageContentList);
        result.ChatContent = new AIStreamChatContent()
        {
            SerialNumber = chunkNumber,
            ResponseContent = stringBuilder.ToString(),
            IsLastChunk = true,
            IsAggregationMsg = true,
            AggregationMsg = completeContent.ToString()
        };

        return result;
    }

    private ChatRole ConvertToChatRole(AuthorRole authorRole)
    {
        if (authorRole == AuthorRole.System)
        {
            return ChatRole.System;
        }

        return authorRole == AuthorRole.Assistant ? ChatRole.Assistant : ChatRole.User;
    }
}