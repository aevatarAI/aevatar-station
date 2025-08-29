using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents.ProxySEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Concurrency;

namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents;

[Description("Sophisticated chat agent that integrates multiple AI models (GPT, Claude, Gemini) with automatic model selection based on query type, load balancing, and fallback mechanisms for optimal user experience.")]
[Reentrant]
public abstract class MultiAIChatGAgent<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IMultiAIChatGAgent
    where TState : MultiAIChatGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : MultiAIChatConfig
{
    protected List<IAIAgentStatusProxy> AIAgentStatusProxies = new();

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Multi-AI Chat Agent");
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        if (configuration.LLMConfigs.IsNullOrEmpty())
        {
            Logger.LogDebug($"[MultiAIChatGAgent][PerformConfigAsync] LLMConfigs is null or empty.");
            return;
        }

        var aiAgentIds = new List<Guid>();
        Logger.LogDebug($"[MultiAIChatGAgent][PerformConfigAsync] LLMConfigs.count={configuration.LLMConfigs.Count}");
        foreach (var llmConfigDto in configuration.LLMConfigs)
        {
            var aiAgentStatusProxy =
                GrainFactory
                    .GetGrain<IAIAgentStatusProxy>(Guid.NewGuid());
            await aiAgentStatusProxy.ConfigAsync(new AIAgentStatusProxyConfig
            {
                Instructions = configuration.Instructions,
                LLMConfig = llmConfigDto,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig,
                RequestRecoveryDelay = configuration.RequestRecoveryDelay,
                ParentId = this.GetPrimaryKey()
            });

            Logger.LogDebug(
                $"[MultiAIChatGAgent][PerformConfigAsync] MultiAIChatgAgentId: {this.GetPrimaryKey().ToString()}, AIAgentStatusProxyId: {aiAgentStatusProxy.GetPrimaryKey().ToString()}");

            AIAgentStatusProxies.Add(aiAgentStatusProxy);
            aiAgentIds.Add(aiAgentStatusProxy.GetPrimaryKey());
        }

        var maxHistoryCount = configuration.MaxHistoryCount;
        if (maxHistoryCount > 100)
        {
            maxHistoryCount = 100;
        }

        if (maxHistoryCount <= 0)
        {
            maxHistoryCount = 10;
        }

        RaiseEvent(new SetMultiAIChatConfigLogEvent
        {
            MaxHistoryCount = maxHistoryCount,
            AIAgentIds = aiAgentIds
        });
        await ConfirmEvents();
    }

    public async Task<List<ChatMessage>?> ChatAsync(string message, ExecutionPromptSettings? promptSettings = null,
        AIChatContextDto? aiChatContextDto = null)
    {
        var aiAgentStatusProxy = await GetAIAgentStatusProxy();
        if (aiAgentStatusProxy == null)
        {
            Logger.LogError($"There is no available AI Agent. {this.GetPrimaryKey().ToString()}");
            throw new SystemException("There is no available AI Agent.");
        }
        
        var result = await aiAgentStatusProxy.ChatWithHistory(message, State.ChatHistory, promptSettings, context: aiChatContextDto);
        
        if (result is not { Count: > 0 }) return result;

        var chatMessages = new List<ChatMessage>();
        chatMessages.Add(new ChatMessage() { ChatRole = ChatRole.User, Content = message });
        chatMessages.AddRange(result);

        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });

        await ConfirmEvents();

        return result;
    }

    public Task<List<ChatMessage>> GetChatMessageAsync()
    {
        return Task.FromResult(State.ChatHistory);
    }

    public async Task<List<ChatMessage>?> ChatWithStreamingAsync(string message, ExecutionPromptSettings? promptSettings = null,
        AIChatContextDto? aiChatContextDto = null)
    {
        var aiAgentStatusProxy = await GetAIAgentStatusProxy();
        if (aiAgentStatusProxy == null)
        {
            Logger.LogError($"There is no available AI Agent. {this.GetPrimaryKey().ToString()}");
            throw new SystemException("There is no available AI Agent.");
        }
        
        var result = await aiAgentStatusProxy.PromptWithStreamAsync(message, State.ChatHistory, promptSettings, context: aiChatContextDto);

        if (!result)
        {
            Logger.LogError($"Failed to initiate streaming response. {this.GetPrimaryKey().ToString()}");
            throw new SystemException("Failed to initiate streaming response.");
        }
        
        return new List<ChatMessage>();
    }

    public async Task<List<ChatMessage>?> CallBackAsync(List<ChatMessage>? messages)
    {
        return messages;
    }
    
    private async Task<IAIAgentStatusProxy?> GetAIAgentStatusProxy()
    {
        if (AIAgentStatusProxies.IsNullOrEmpty())
        {
            return null;
        }

        foreach (var aiAgentStatusProxy in AIAgentStatusProxies)
        {
            if (!await aiAgentStatusProxy.IsAvailableAsync())
            {
                continue;
            }

            return aiAgentStatusProxy;
        }
        return null;
    }
    
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        if (!State.AIAgentIds.IsNullOrEmpty())
        {
            Logger.LogDebug(
                $"[MultiAIChatGAgent][OnGAgentActivateAsync] init AIAgentStatusProxies..{JsonConvert.SerializeObject(State.AIAgentIds)}");
            AIAgentStatusProxies =
                new List<IAIAgentStatusProxy>();
            foreach (var agentId in State.AIAgentIds)
            {
                AIAgentStatusProxies.Add(GrainFactory
                    .GetGrain<IAIAgentStatusProxy>(agentId));
            }
        }

        return Task.CompletedTask;
    }


    [GenerateSerializer]
    public class AddChatHistoryLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<ChatMessage> ChatList { get; set; }
    }

    [GenerateSerializer]
    public class SetMultiAIChatConfigLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public int MaxHistoryCount { get; set; }
        [Id(1)] public List<Guid> AIAgentIds { get; set; }
    }

    protected override void GAgentTransitionState(TState state,
        StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddChatHistoryLogEvent setChatHistoryLog:
                if (setChatHistoryLog.ChatList.Count > 0)
                {
                    state.ChatHistory.AddRange(setChatHistoryLog.ChatList);
                }

                if (state.ChatHistory.Count() > state.MaxHistoryCount)
                {
                    state.ChatHistory.RemoveRange(0, state.ChatHistory.Count() - state.MaxHistoryCount);
                }

                break;
            case SetMultiAIChatConfigLogEvent setMultiAiChatConfigLogEvent:
                state.MaxHistoryCount = setMultiAiChatConfigLogEvent.MaxHistoryCount;
                break;
        }
    }
}

public interface IMultiAIChatGAgent : IGAgent
{
    Task<List<ChatMessage>?> ChatAsync(string message,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? aiChatContextDto = null);

    Task<List<ChatMessage>?> ChatWithStreamingAsync(string message, ExecutionPromptSettings? promptSettings = null,
        AIChatContextDto? aiChatContextDto = null);

    Task<List<ChatMessage>?> CallBackAsync(List<ChatMessage>? messages);
    
    Task<List<ChatMessage>> GetChatMessageAsync();
}