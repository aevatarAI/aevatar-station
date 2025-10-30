using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.SyncWork;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.AIGAgent.Agent;

/// <summary>
/// AIGAgentBasePlus inherits from BusinessAgentBase for business functionality + AI capabilities
/// This creates the proper hierarchy: BusinessAgentBase (business logic) -> AIGAgentBasePlus (business + AI)
/// Uses unified Event class for event forwarding
/// </summary>
public abstract partial class
    AIGAgentBasePlus<TState, TStateLogEvent, TConfiguration> :
    BusinessAgentBase<TState, TStateLogEvent, TConfiguration>, IAIGAgent, IGrainAsyncHandler<AIStreamChatResponseEvent>
    where TState : AIGAgentStateBasePlus, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TConfiguration : ConfigurationBase
{
    protected async Task<bool> PromptWithStreamAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null, bool ifAsync = true, List<string>? imageKeys = null)
    {
        // Resolve the LLM configuration from centralized config if needed
        var llmConfig = await GetLLMConfigAsync();
        if (llmConfig == null)
        {
            Logger.LogError("Failed to resolve LLM configuration for stream async request");
            return false;
        }

        var request = new AIStreamChatRequest()
        {
            LlmConfig = llmConfig,
            Instructions = State.PromptTemplate,
            VectorId = this.GetGrainId().ToString().Replace("/", ""),
            StreamingConfig = State.StreamingConfig,
            Content = prompt,
            History = history,
            IfUseKnowledge = State.IfUpsertKnowledge,
            PromptSettings = promptSettings,
            Context = context,
            ImageKeys = imageKeys
        };

        return await CreateLongRunTaskAsync<AIStreamChatRequest, AIStreamChatResponseEvent>(request, ifAsync);
    }

    protected async Task<bool> CreateLongRunTaskAsync<TRequest, TResponse>(TRequest request, bool ifAsync = true)
    {
        try
        {
            var syncWorker = GrainFactory.GetGrain<IGrainAsyncWorker<TRequest, TResponse>>(Guid.NewGuid());
            await syncWorker.SetLongRunTaskAsync(this.GetGrainId());
            if (ifAsync == true)
            {
                var result = await syncWorker.Start(request);
                if (result == false)
                {
                    Logger.LogError(
                        $"CreateStreamLongRunTaskAsync run task fail, request info:{JsonConvert.SerializeObject(request)}");
                }

                return result;
            }

            await syncWorker.StartWorkAndPollUntilResult(request);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"CreateStreamLongRunTaskAsync creating long run task error: {ex.Message}");
            throw;
        }
    }

    public async Task HandleStreamAsync(AIStreamChatResponseEvent arg)
    {
        if (arg.TokenUsageStatistics != null)
        {
            var tokenUsage = new TokenUsageStateLogEvent()
            {
                GrainId = this.GetPrimaryKey(),
                InputToken = arg.TokenUsageStatistics.InputToken,
                OutputToken = arg.TokenUsageStatistics.OutputToken,
                TotalUsageToken = arg.TokenUsageStatistics.TotalUsageToken,
                CreateTime = arg.TokenUsageStatistics.CreateTime
            };

            RaiseEvent(tokenUsage);
        }

        await AIChatHandleStreamAsync(arg.Context, arg.ErrorEnum, arg.ErrorMessage, arg.ChatContent);
    }

    protected virtual Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        return Task.CompletedTask;
    }
}