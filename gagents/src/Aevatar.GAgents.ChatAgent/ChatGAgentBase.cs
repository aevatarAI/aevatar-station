using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent.State;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.Threading;
using ChatMessage = Aevatar.GAgents.AI.Common.ChatMessage;
using ChatRole = Aevatar.GAgents.AI.Common.ChatRole;

namespace Aevatar.GAgents.ChatAgent.GAgent;

public abstract class
    ChatGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IChatAgent
    where TState : ChatGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ChatConfigDto
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat Agent");
    }

    public async Task<List<ChatMessage>?> ChatAsync(string message, ExecutionPromptSettings? promptSettings = null,
        AIChatContextDto? aiChatContextDto = null, List<string>? imageKeys = null)
    {
        var result = await ChatWithHistoryAndToolsAsync(message, State.ChatHistory, promptSettings,
            context: aiChatContextDto,
            imageKeys: imageKeys);

        if (result.Response.IsNullOrEmpty())
        {
            return new List<ChatMessage>();
        }

        var assistantMessage = new ChatMessage
        {
            ChatRole = ChatRole.Assistant,
            Content = result.Response,
        };
        var chatMessages = new List<ChatMessage>
        {
            new()
            {
                ChatRole = ChatRole.User,
                Content = message,
                ImageKeys = imageKeys
            },
            assistantMessage
        };

        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });

        await ConfirmEvents();

        return [assistantMessage];
    }

    public async Task<bool> ChatWithStreamAsync(string message, AIChatContextDto context,
        ExecutionPromptSettings? promptSettings = null, List<string>? imageKeys = null)
    {
        var result =
            await PromptWithStreamAsync(message, State.ChatHistory, promptSettings, context, imageKeys: imageKeys);
        if (!result) return result;

        var chatMessages = new List<ChatMessage>();
        chatMessages.Add(new ChatMessage() { ChatRole = ChatRole.User, Content = message, ImageKeys = imageKeys });
        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });
        await ConfirmEvents();

        return result;
    }

    protected sealed override async Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        if (content is { IsAggregationMsg: true })
        {
            RaiseEvent(new AddChatHistoryLogEvent()
            {
                ChatList = new List<ChatMessage>()
                    { new ChatMessage() { ChatRole = ChatRole.Assistant, Content = content.ResponseContent } }
            });

            await ConfirmEvents();
        }

        await HandleChatStreamAsync(context, errorEnum, errorMessage, content);
    }

    protected virtual Task HandleChatStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        return Task.CompletedTask;
    }

    protected sealed override async Task PerformConfigAsync(TConfiguration configuration)
    {
        await InitializeAsync(
            new InitializeDto()
            {
                Instructions = configuration.Instructions,
                LLMConfig = configuration.LLMConfig,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig
            });
        var maxHistoryCount = configuration.MaxHistoryCount;
        if (maxHistoryCount > 100)
        {
            maxHistoryCount = 100;
        }

        if (maxHistoryCount == 0)
        {
            maxHistoryCount = 10;
        }

        RaiseEvent(new SetMaxHistoryCount() { MaxHistoryCount = maxHistoryCount });
        await ConfirmEvents();

        await ChatPerformConfigAsync(configuration);
    }

    protected virtual Task ChatPerformConfigAsync(TConfiguration configuration)
    {
        return Task.CompletedTask;
    }

    [GenerateSerializer]
    public class AddChatHistoryLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<ChatMessage> ChatList { get; set; }
    }

    [GenerateSerializer]
    public class SetMaxHistoryCount : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public int MaxHistoryCount { get; set; }
    }

    protected override void AIGAgentTransitionState(TState state,
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
                    var toDeleteImageKeys = new List<string>();
                    var recordsToDelete = state.ChatHistory.Take(state.ChatHistory.Count() - state.MaxHistoryCount);
                    foreach (var record in recordsToDelete)
                    {
                        if (record.ImageKeys != null && record.ImageKeys.Count > 0)
                        {
                            toDeleteImageKeys.AddRange(record.ImageKeys);
                        }
                    }

                    if (toDeleteImageKeys.Any())
                    {
                        var blobContainer = ServiceProvider.GetRequiredService<IBlobContainer>();
                        var downloadTasks = toDeleteImageKeys.Select(async key =>
                        {
                            await blobContainer.DeleteAsync(key);
                        });

                        AsyncHelper.RunSync(async () => await Task.WhenAll(downloadTasks));
                    }

                    state.ChatHistory.RemoveRange(0, state.ChatHistory.Count() - state.MaxHistoryCount);
                }

                break;
            case SetMaxHistoryCount setMaxHistoryCount:
                state.MaxHistoryCount = setMaxHistoryCount.MaxHistoryCount;
                break;
        }
    }
}