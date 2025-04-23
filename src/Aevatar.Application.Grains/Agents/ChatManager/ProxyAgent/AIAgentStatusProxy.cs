using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent.Dtos;
using Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent.ProxySEvents;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent;

[GAgent]
[Reentrant]
public class AIAgentStatusProxy :
    AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>,
    IAIAgentStatusProxy
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AIGAgent supporting state management");
    }

    protected sealed override async Task PerformConfigAsync(AIAgentStatusProxyConfig configuration)
    {
        await InitializeAsync(
            new InitializeDto()
            {
                Instructions = configuration.Instructions,
                LLMConfig = configuration.LLMConfig,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig
            });
        RaiseEvent(new SetStatusProxyConfigLogEvent
        {
            RecoveryDelay = configuration.RequestRecoveryDelay,
            ParentId = configuration.ParentId
        });
        await ConfirmEvents();
    }

    public async Task<List<ChatMessage>?> ChatWithHistory(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null)
    {
        return await base.ChatWithHistory(prompt, history, promptSettings, context: context);
    }

    public async Task<bool> PromptWithStreamAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null)
    {
        return await base.PromptWithStreamAsync(prompt, history, promptSettings, context);
    }

    protected override async Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        Logger.LogDebug(
            $"[AIAgentStatusProxy][AIChatHandleStreamAsync] sessionId {context?.RequestId.ToString()}, chatId {context?.ChatId}, errorEnum {errorEnum}, errorMessage {errorMessage}: {JsonConvert.SerializeObject(content)}");
        if (errorEnum == AIExceptionEnum.RequestLimitError)
        {
            RaiseEvent(new SetAvailableLogEvent
            {
                IsAvailable = false,
                ExceptionCount = 1
            });
            await ConfirmEvents();
        }
        
        var godChat = GrainFactory.GetGrain<IGodChat>(State.ParentId);
        await godChat.ChatMessageCallbackAsync(context, errorEnum, errorMessage, content);
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (State.IsAvailable)
        {
            return true;
        }

        if (State.UnavailableSince == null)
        {
            Logger.LogDebug($"[AIAgentStatusProxy][IsAvailableAsync] State.UnavailableSince is null");
            return true;
        }

        var now = DateTime.UtcNow;
        var unavailableSince = State.UnavailableSince;
        var timeElapsed = now - unavailableSince;
        if (timeElapsed > State.RecoveryDelay)
        {
            RaiseEvent(new SetAvailableLogEvent
            {
                IsAvailable = true
            });
            await ConfirmEvents();
            return true;
        }

        return false;
    }

    protected override void AIGAgentTransitionState(AIAgentStatusProxyState state,
        StateLogEventBase<AIAgentStatusProxyLogEvent> @event)
    {
        switch (@event)
        {
            case SetStatusProxyConfigLogEvent setStatusProxyConfigLogEvent:
                if (setStatusProxyConfigLogEvent.RecoveryDelay != null)
                {
                    State.RecoveryDelay = (TimeSpan)setStatusProxyConfigLogEvent.RecoveryDelay;
                }

                State.ParentId = setStatusProxyConfigLogEvent.ParentId;
                break;
            case SetAvailableLogEvent setAvailableLogEvent:
                State.IsAvailable = setAvailableLogEvent.IsAvailable;
                if (State.IsAvailable)
                {
                    State.UnavailableSince = null;
                }
                else
                {
                    State.UnavailableSince = DateTime.UtcNow;
                    State.UnavailableCount += 1;
                    State.ExceptionCount += setAvailableLogEvent.ExceptionCount;
                }
                break;
        }
    }
}

public interface IAIAgentStatusProxy : IGAgent, IAIGAgent
{
    Task<bool> IsAvailableAsync();

    Task<List<ChatMessage>?> ChatWithHistory(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null);

    Task<bool> PromptWithStreamAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null);
}