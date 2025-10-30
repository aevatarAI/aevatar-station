using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.AIHttpAsyncWoker;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.AIGAgent.Agent;

/// <summary>
/// AIGAgentBasePlus inherits from BusinessAgentBase for layered architecture
/// Uses unified Event class for event forwarding
/// </summary>
public abstract partial class
    AIGAgentBasePlus<TState, TStateLogEvent, TConfiguration> :
    BusinessAgentBase<TState, TStateLogEvent, TConfiguration>, IAIGAgent, IGrainAsyncHandler<AIHttpAsyncResponse>
    where TState : AIGAgentStateBasePlus, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TConfiguration : ConfigurationBase
{
    protected async Task<bool> PromptHttpAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null, bool ifAsync = true, List<string>? imageKeys = null)
    {
        // Resolve the LLM configuration from centralized config if needed
        var llmConfig = await GetLLMConfigAsync();
        if (llmConfig == null)
        {
            Logger.LogError("Failed to resolve LLM configuration for HTTP async request");
            return false;
        }

        var request = new AIHttpAsyncRequest()
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

        return await CreateLongRunTaskAsync<AIHttpAsyncRequest, AIHttpAsyncResponse>(request, ifAsync);
    }

    public async Task HandleStreamAsync(AIHttpAsyncResponse arg)
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

        await AIChatHttpResponseHandleAsync(arg.Context, arg.ErrorEnum, arg.ErrorMessage, arg.ResponseContent);
    }

    protected virtual Task AIChatHttpResponseHandleAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        string? content)
    {
        return Task.CompletedTask;
    }
}