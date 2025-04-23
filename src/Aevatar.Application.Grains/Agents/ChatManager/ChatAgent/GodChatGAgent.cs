using System.Diagnostics;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent;
using Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent.Dtos;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Concurrency;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[Description("god chat agent")]
[GAgent]
[Reentrant]
public class GodChatGAgent : ChatGAgentBase<GodChatState, GodChatEventLog, EventBase, ChatConfigDto>, IGodChat
{
    private List<IAIAgentStatusProxy> AIAgentStatusProxies = new();
    private static readonly List<string> UsableLLMs = new List<string>() { "OpenAILast", "OpenAI" };
    private static readonly TimeSpan RequestRecoveryDelay = TimeSpan.FromSeconds(600);

    protected override async Task ChatPerformConfigAsync(ChatConfigDto configuration)
    {
        if (UsableLLMs.IsNullOrEmpty())
        {
            Logger.LogDebug($"[GodChatGAgent][ChatPerformConfigAsync] LLMConfigs is null or empty.");
            return;
        }

        var aiAgentIds = new List<Guid>();
        Logger.LogDebug(
            $"[GodChatGAgent][ChatPerformConfigAsync] LLMConfigs: {JsonConvert.SerializeObject(UsableLLMs)}");
        foreach (var usableLlM in UsableLLMs)
        {
            var aiAgentStatusProxy =
                GrainFactory
                    .GetGrain<IAIAgentStatusProxy>(Guid.NewGuid());
            await aiAgentStatusProxy.ConfigAsync(new AIAgentStatusProxyConfig
            {
                Instructions = configuration.Instructions,
                LLMConfig = new LLMConfigDto
                {
                    SystemLLM = usableLlM
                },
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig,
                RequestRecoveryDelay = RequestRecoveryDelay,
                ParentId = this.GetPrimaryKey()
            });

            Logger.LogDebug(
                $"[GodChatGAgent][ChatPerformConfigAsync] primaryKey: {this.GetPrimaryKey().ToString()}, LLM: {usableLlM}, AIAgentStatusProxyId: {aiAgentStatusProxy.GetPrimaryKey().ToString()}");

            AIAgentStatusProxies.Add(aiAgentStatusProxy);
            aiAgentIds.Add(aiAgentStatusProxy.GetPrimaryKey());
        }

        RaiseEvent(new SetAIAgentIdLogEvent
        {
            AIAgentIds = aiAgentIds
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestStreamChatEvent @event)
    {
        string chatId = Guid.NewGuid().ToString();
        Logger.LogDebug(
            $"[GodChatGAgent][RequestStreamGodChatEvent] start:{JsonConvert.SerializeObject(@event)} chatID:{chatId}");
        var title = "";
        var content = "";
        var isLastChunk = false;

        try
        {
            if (State.StreamingModeEnabled)
            {
                Logger.LogDebug("State.StreamingModeEnabled is on");
                await StreamChatWithSessionAsync(@event.SessionId, @event.SystemLLM, @event.Content, chatId);
            }
            else
            {
                var response = await ChatWithSessionAsync(@event.SessionId, @event.SystemLLM, @event.Content);
                content = response.Item1;
                title = response.Item2;
                isLastChunk = true;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[GodChatGAgent][RequestStreamGodChatEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseStreamGodChat()
        {
            ChatId = chatId,
            Response = content,
            NewTitle = title,
            IsLastChunk = isLastChunk,
            SerialNumber = -1,
            SessionId = @event.SessionId
        });

        Logger.LogDebug($"[GodChatGAgent][RequestStreamGodChatEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    public async Task StreamChatWithSessionAsync(Guid sessionId, string sysmLLM, string content, string chatId,
        ExecutionPromptSettings promptSettings = null, bool isHttpRequest = false)
    {
        Stopwatch sw = new Stopwatch();
        Logger.LogDebug($"StreamChatWithSessionAsync {sessionId.ToString()} - step1,time use:{sw.ElapsedMilliseconds}");

        var title = "";

        if (State.Title.IsNullOrEmpty())
        {
            sw.Start();
            title = string.Join(" ", content.Split(" ").Take(4));

            RaiseEvent(new RenameChatTitleEventLog()
            {
                Title = title
            });

            await ConfirmEvents();

            sw.Stop();
            IChatManagerGAgent chatManagerGAgent =
                GrainFactory.GetGrain<IChatManagerGAgent>((Guid)State.ChatManagerGuid);
            await chatManagerGAgent.RenameChatTitleAsync(new RenameChatTitleEvent()
            {
                SessionId = sessionId,
                Title = title
            });
            Logger.LogDebug(
                $"StreamChatWithSessionAsync {sessionId.ToString()} - step3,time use:{sw.ElapsedMilliseconds}");
        }

        sw.Reset();
        sw.Start();
        var configuration = GetConfiguration();
        await GodStreamChatAsync(sessionId, await configuration.GetSystemLLM(),
            await configuration.GetStreamingModeEnabled(),
            content, chatId, promptSettings, isHttpRequest);
        sw.Stop();
        Logger.LogDebug($"StreamChatWithSessionAsync {sessionId.ToString()} - step4,time use:{sw.ElapsedMilliseconds}");
    }

    public async Task<string> GodStreamChatAsync(Guid sessionId, string llm, bool streamingModeEnabled, string message,
        string chatId,
        ExecutionPromptSettings? promptSettings = null, bool isHttpRequest = false)
    {
        var configuration = GetConfiguration();

        var sysMessage = await configuration.GetPrompt();

        if (State.SystemLLM != llm || State.StreamingModeEnabled != streamingModeEnabled)
        {
            var initializeDto = new InitializeDto()
            {
                Instructions = sysMessage, LLMConfig = new LLMConfigDto() { SystemLLM = llm },
                StreamingModeEnabled = true, StreamingConfig = new StreamingConfig()
                {
                    BufferingSize = 32
                }
            };
            Logger.LogDebug(
                $"[GodChatGAgent][GodStreamChatAsync] Detail : {JsonConvert.SerializeObject(initializeDto)}");

            await InitializeAsync(initializeDto);
        }

        var aiChatContextDto = new AIChatContextDto()
        {
            ChatId = chatId,
            RequestId = sessionId
        };
        if (isHttpRequest)
        {
            aiChatContextDto.MessageId = JsonConvert.SerializeObject(new Dictionary<string, object>()
            {
                { "IsHttpRequest", true }, { "LLM", llm }, { "StreamingModeEnabled", streamingModeEnabled },
                { "Message", message }
            });
        }

        var aiAgentStatusProxy = await GetAIAgentStatusProxy();
        if (aiAgentStatusProxy != null)
        {
            Logger.LogDebug(
                $"[GodChatGAgent][GodStreamChatAsync] agent {aiAgentStatusProxy.GetPrimaryKey().ToString()}, session {sessionId.ToString()}, chat {chatId}");
            
            //TODO Stress testing
            // var settings = promptSettings ?? new ExecutionPromptSettings();
            // settings.Temperature = "0.9";
            // var result = await aiAgentStatusProxy.PromptWithStreamAsync(message, State.ChatHistory, settings,
            //     context: aiChatContextDto);
            // if (!result)
            // {
            //     Logger.LogError($"Failed to initiate streaming response. {this.GetPrimaryKey().ToString()}");
            // }
            MockCallBackAsync(sessionId, llm, message, chatId, promptSettings, isHttpRequest);
            

            RaiseEvent(new AddChatHistoryLogEvent
            {
                ChatList = new List<ChatMessage>()
                {
                    new ChatMessage
                    {
                        ChatRole = ChatRole.User,
                        Content = message
                    }
                }
            });

            await ConfirmEvents();
        }
        else
        {
            Logger.LogDebug(
                $"[GodChatGAgent][GodStreamChatAsync] history agent, session {sessionId.ToString()}, chat {chatId}");
            //TODO Stress testing
            //await ChatAsync(message, promptSettings, aiChatContextDto);
        }

        return string.Empty;
    }
    
    private async Task MockCallBackAsync(Guid sessionId, string sysmLLM, string content, string chatId,
        ExecutionPromptSettings promptSettings = null, bool isHttpRequest = false)
    {
        try
        {
            Logger.LogDebug(
                $"[GodChatGAgent][MockCallBackAsync] Mock callback, session {sessionId.ToString()}, chat {chatId}");
            await Task.Delay(TimeSpan.FromMilliseconds(800));

            await ChatMessageCallbackAsync(new AIChatContextDto
            {
                RequestId = sessionId,
                MessageId = JsonConvert.SerializeObject(new Dictionary<string, object>()
                {
                    { "IsHttpRequest", true }, { "LLM", sysmLLM }, { "StreamingModeEnabled", true },
                    { "Message", content }
                }),
                ChatId = chatId
            }, AIExceptionEnum.None, null, new AIStreamChatContent
            {
                ResponseContent = "Mock data for stress testing environment.",
                SerialNumber = 1,
                IsLastChunk = true,
                IsAggregationMsg = true,
                AggregationMsg = "Mock data for stress testing environment."
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                $"[GodChatGAgent][MockCallBackAsync] Mock callback error, session {sessionId.ToString()}, chat {chatId}, {e.Message}");
        }
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

    public async Task SetUserProfileAsync(UserProfileDto? userProfileDto)
    {
        if (userProfileDto == null)
        {
            return;
        }

        RaiseEvent(new UpdateUserProfileGodChatEventLog
        {
            Gender = userProfileDto.Gender,
            BirthDate = userProfileDto.BirthDate,
            BirthPlace = userProfileDto.BirthPlace,
            FullName = userProfileDto.FullName
        });

        await ConfirmEvents();
    }

    public async Task<UserProfileDto?> GetUserProfileAsync()
    {
        if (State.UserProfile == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Gender = State.UserProfile.Gender,
            BirthDate = State.UserProfile.BirthDate,
            BirthPlace = State.UserProfile.BirthPlace,
            FullName = State.UserProfile.FullName
        };
    }

    public async Task<string> GodChatAsync(string llm, string message,
        ExecutionPromptSettings? promptSettings = null)
    {
        if (State.SystemLLM != llm)
        {
            await InitializeAsync(new InitializeDto()
                { Instructions = State.PromptTemplate, LLMConfig = new LLMConfigDto() { SystemLLM = llm } });
        }

        //TODO Stress testing
        //var response = await ChatAsync(message, promptSettings);
        var response = new List<ChatMessage>();
        
        if (response is { Count: > 0 })
        {
            return response[0].Content!;
        }

        return string.Empty;
    }


    public async Task InitAsync(Guid ChatManagerGuid)
    {
        RaiseEvent(new SetChatManagerGuidEventLog
        {
            ChatManagerGuid = ChatManagerGuid
        });

        await ConfirmEvents();
    }

    public async Task ChatMessageCallbackAsync(AIChatContextDto contextDto,
        AIExceptionEnum aiExceptionEnum, string? errorMessage, AIStreamChatContent? chatContent)
    {
        if (aiExceptionEnum == AIExceptionEnum.RequestLimitError && !contextDto.MessageId.IsNullOrWhiteSpace())
        {
            Logger.LogError(
                $"[GodChatGAgent][ChatMessageCallbackAsync] RequestLimitError retry. contextDto {JsonConvert.SerializeObject(contextDto)}");
            var configuration = GetConfiguration();
            var systemLlm = await configuration.GetSystemLLM();
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(contextDto.MessageId);
            GodStreamChatAsync(contextDto.RequestId,
                (string)dictionary.GetValueOrDefault("LLM", systemLlm),
                (bool)dictionary.GetValueOrDefault("StreamingModeEnabled", true),
                (string)dictionary.GetValueOrDefault("Message", string.Empty),
                contextDto.ChatId, null, (bool)dictionary.GetValueOrDefault("IsHttpRequest", true));
            return;
        }
        else if (aiExceptionEnum != AIExceptionEnum.None)
        {
            Logger.LogError(
                $"[GodChatGAgent][ChatMessageCallbackAsync] stream error. sessionId {contextDto?.RequestId.ToString()}, chatId {contextDto?.ChatId}, error {aiExceptionEnum}");
            var chatMessage = new ResponseStreamGodChat()
            {
                Response =
                    "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak.",
                ChatId = contextDto.ChatId,
                IsLastChunk = true,
                SerialNumber = -2
            };
            if (contextDto.MessageId.IsNullOrWhiteSpace())
            {
                await PublishAsync(chatMessage);
                return;
            }

            await PushMessageToClientAsync(chatMessage);
            return;
        }

        if (chatContent == null)
        {
            Logger.LogError(
                $"[GodChatGAgent][ChatMessageCallbackAsync] return null. sessionId {contextDto.RequestId.ToString()},chatId {contextDto.ChatId},aiExceptionEnum:{aiExceptionEnum}, errorMessage:{errorMessage}");
            return;
        }

        Logger.LogDebug(
            $"[GodChatGAgent][ChatMessageCallbackAsync] sessionId {contextDto.RequestId.ToString()}, chatId {contextDto.ChatId}, messageId {contextDto.MessageId}, {JsonConvert.SerializeObject(chatContent)}");
        if (chatContent.IsAggregationMsg)
        {
            RaiseEvent(new AddChatHistoryLogEvent
            {
                ChatList = new List<ChatMessage>()
                {
                    new ChatMessage
                    {
                        ChatRole = ChatRole.Assistant,
                        Content = chatContent?.AggregationMsg
                    }
                }
            });

            await ConfirmEvents();
        }

        var partialMessage = new ResponseStreamGodChat()
        {
            Response = chatContent.ResponseContent,
            ChatId = contextDto.ChatId,
            IsLastChunk = chatContent.IsLastChunk,
            SerialNumber = chatContent.SerialNumber,
            SessionId = contextDto.RequestId
        };
        if (contextDto.MessageId.IsNullOrWhiteSpace())
        {
            await PublishAsync(partialMessage);
            return;
        }

        await PushMessageToClientAsync(partialMessage);
    }

    private async Task PushMessageToClientAsync(ResponseStreamGodChat chatMessage)
    {
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace, this.GetPrimaryKey());
        Logger.LogDebug(
            $"[GodChatGAgent][PushMessageToClientAsync] sessionId {this.GetPrimaryKey().ToString()}, namespace {AevatarOptions!.StreamNamespace}, streamId {streamId.ToString()}");
        var stream = StreamProvider.GetStream<ResponseStreamGodChat>(streamId);
        await stream.OnNextAsync(chatMessage);
    }

    public Task<List<ChatMessage>> GetChatMessageAsync()
    {
        Logger.LogDebug(
            $"[ChatGAgentManager][GetSessionMessageListAsync] - session:ID {this.GetPrimaryKey().ToString()} ,message={JsonConvert.SerializeObject(State.ChatHistory)}");
        return Task.FromResult(State.ChatHistory);
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        if (!State.AIAgentIds.IsNullOrEmpty())
        {
            Logger.LogDebug(
                $"[GodChatGAgent][OnAIGAgentActivateAsync] init AIAgentStatusProxies..{JsonConvert.SerializeObject(State.AIAgentIds)}");
            AIAgentStatusProxies =
                new List<IAIAgentStatusProxy>();
            foreach (var agentId in State.AIAgentIds)
            {
                AIAgentStatusProxies.Add(GrainFactory
                    .GetGrain<IAIAgentStatusProxy>(agentId));
            }
        }
    }

    protected sealed override void AIGAgentTransitionState(GodChatState state,
        StateLogEventBase<GodChatEventLog> @event)
    {
        base.AIGAgentTransitionState(state, @event);

        switch (@event)
        {
            case UpdateUserProfileGodChatEventLog updateUserProfileGodChatEventLog:
                if (State.UserProfile == null)
                {
                    State.UserProfile = new UserProfile();
                }

                State.UserProfile.Gender = updateUserProfileGodChatEventLog.Gender;
                State.UserProfile.BirthDate = updateUserProfileGodChatEventLog.BirthDate;
                State.UserProfile.BirthPlace = updateUserProfileGodChatEventLog.BirthPlace;
                State.UserProfile.FullName = updateUserProfileGodChatEventLog.FullName;
                break;
            case RenameChatTitleEventLog renameChatTitleEventLog:
                State.Title = renameChatTitleEventLog.Title;
                break;
            case SetChatManagerGuidEventLog setChatManagerGuidEventLog:
                State.ChatManagerGuid = setChatManagerGuidEventLog.ChatManagerGuid;
                break;
            case SetAIAgentIdLogEvent setAiAgentIdLogEvent:
                State.AIAgentIds = setAiAgentIdLogEvent.AIAgentIds;
                break;
        }
    }

    private IConfigurationGAgent GetConfiguration()
    {
        return GrainFactory.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
    }

    public async Task<Tuple<string, string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null)
    {
        var title = "";
        if (State.Title.IsNullOrEmpty())
        {
            // var titleList = await ChatWithHistory(content);
            // title = titleList is { Count: > 0 }
            //     ? titleList[0].Content!
            //     : string.Join(" ", content.Split(" ").Take(4));

            title = string.Join(" ", content.Split(" ").Take(4));

            RaiseEvent(new RenameChatTitleEventLog()
            {
                Title = title
            });

            await ConfirmEvents();

            IChatManagerGAgent chatManagerGAgent =
                GrainFactory.GetGrain<IChatManagerGAgent>((Guid)State.ChatManagerGuid);
            await chatManagerGAgent.RenameChatTitleAsync(new RenameChatTitleEvent()
            {
                SessionId = sessionId,
                Title = title
            });
        }

        var configuration = GetConfiguration();
        var response = await GodChatAsync(await configuration.GetSystemLLM(), content, promptSettings);
        return new Tuple<string, string>(response, title);
    }
}