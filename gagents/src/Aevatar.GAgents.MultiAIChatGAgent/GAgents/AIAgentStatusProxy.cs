using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents.ProxySEvents;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents;

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
        if (errorEnum == AIExceptionEnum.RequestLimitError)
        {
            // RaiseEvent(new SetStatusProxyConfigLogEvent
            // {
            //     RecoveryDelay = configuration.RequestRecoveryDelay,
            //     ParentId = configuration.ParentId
            // });
            // await ConfirmEvents();
        }
        
        var multiAiChatGAgent = GrainFactory.GetGrain<IMultiAIChatGAgent>(State.ParentId);
        await multiAiChatGAgent.CallBackAsync(new List<ChatMessage>()
            {
                new ChatMessage
                {
                    ChatRole = ChatRole.Assistant,
                    Content = content?.ResponseContent
                }
            }
        );
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
            RaiseEvent(new SetAvailableLogEvent());
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
                    state.RecoveryDelay = (TimeSpan)setStatusProxyConfigLogEvent.RecoveryDelay;
                }

                state.ParentId = setStatusProxyConfigLogEvent.ParentId;
                break;
            case SetAvailableLogEvent setAvailableLogEvent:
                state.IsAvailable = true;
                state.UnavailableSince = null;
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