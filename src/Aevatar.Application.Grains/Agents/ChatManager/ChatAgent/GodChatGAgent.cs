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
    private static readonly Dictionary<string, List<string>> RegionToLLMsMap = new Dictionary<string, List<string>>()
    {
        //"SkyLark-Pro-250415"
        { "CN", new List<string> { "BytePlusDeepSeekV3"} },
        { "DEFAULT", new List<string>() {  "OpenAILast", "OpenAI" }}
    };
    private static readonly TimeSpan RequestRecoveryDelay = TimeSpan.FromSeconds(600);
    private const string DefaultRegion = "DEFAULT";

    protected override async Task ChatPerformConfigAsync(ChatConfigDto configuration)
    {
        if (RegionToLLMsMap.IsNullOrEmpty())
        {
            Logger.LogDebug($"[GodChatGAgent][ChatPerformConfigAsync] LLMConfigs is null or empty.");
            return;
        }
        
        var proxyIds = await InitializeRegionProxiesAsync(DefaultRegion);;
        Dictionary<string, List<Guid>> regionProxies = new();
        regionProxies[DefaultRegion] = proxyIds;
        RaiseEvent(new UpdateRegionProxiesLogEvent
        {
            RegionProxies = regionProxies
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
        ExecutionPromptSettings promptSettings = null, bool isHttpRequest = false, string? region = null)
    {
        var sw = new Stopwatch();
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
            content, chatId, promptSettings, isHttpRequest, region);
        sw.Stop();
        Logger.LogDebug($"StreamChatWithSessionAsync {sessionId.ToString()} - step4,time use:{sw.ElapsedMilliseconds}");
    }

    public async Task<string> GodStreamChatAsync(Guid sessionId, string llm, bool streamingModeEnabled, string message,
        string chatId, ExecutionPromptSettings? promptSettings = null, bool isHttpRequest = false,
        string? region = null)
    {
        var configuration = GetConfiguration();
        var sysMessage = await configuration.GetPrompt();

        await LLMInitializedAsync(llm, streamingModeEnabled, sysMessage);

        var aiChatContextDto =
            CreateAIChatContext(sessionId, llm, streamingModeEnabled, message, chatId, promptSettings, isHttpRequest, region);

        var aiAgentStatusProxy = await GetProxyByRegionAsync(region);
        if (aiAgentStatusProxy != null)
        {
            Logger.LogDebug(
                $"[GodChatGAgent][GodStreamChatAsync] agent {aiAgentStatusProxy.GetPrimaryKey().ToString()}, session {sessionId.ToString()}, chat {chatId}");
            var settings = promptSettings ?? new ExecutionPromptSettings();
            settings.Temperature = "0.9";
            var result = await aiAgentStatusProxy.PromptWithStreamAsync(message, State.ChatHistory, settings,
                context: aiChatContextDto);
            if (!result)
            {
                Logger.LogError($"Failed to initiate streaming response. {this.GetPrimaryKey().ToString()}");
            }

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
            await ChatAsync(message, promptSettings, aiChatContextDto);
        }

        return string.Empty;
    }

    private async Task LLMInitializedAsync(string llm, bool streamingModeEnabled, string sysMessage)
    {
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
    }

    private AIChatContextDto CreateAIChatContext(Guid sessionId, string llm, bool streamingModeEnabled,
        string message, string chatId, ExecutionPromptSettings? promptSettings = null, bool isHttpRequest = false,
        string? region = null)
    {
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
                { "Message", message }, {"Region", region}
            });
        }

        return aiChatContextDto;
    }
    
    private async Task<IAIAgentStatusProxy?> GetProxyByRegionAsync(string? region)
    {
        Logger.LogDebug($"[GodChatGAgent][GetProxyByRegionAsync] session {this.GetPrimaryKey().ToString()}, Region: {region}");
        if (string.IsNullOrWhiteSpace(region))
        {
            return await GetProxyByRegionAsync(DefaultRegion);
        }
        
        if (State.RegionProxies == null || !State.RegionProxies.TryGetValue(region, out var proxyIds) || proxyIds.IsNullOrEmpty())
        {
            Logger.LogDebug($"[GodChatGAgent][GetProxyByRegionAsync] session {this.GetPrimaryKey().ToString()}, No proxies found for region {region}, initializing.");
            proxyIds = await InitializeRegionProxiesAsync(region);
            Dictionary<string, List<Guid>> regionProxies = new()
            {
                {region, proxyIds}
            };
            RaiseEvent(new UpdateRegionProxiesLogEvent
            {
                RegionProxies = regionProxies
            });
            await ConfirmEvents();
        }

        foreach (var proxyId in proxyIds)
        {
            var proxy = GrainFactory.GetGrain<IAIAgentStatusProxy>(proxyId);
            if (await proxy.IsAvailableAsync())
            {
                return proxy;
            }
        }

        Logger.LogDebug($"[GodChatGAgent][GetProxyByRegionAsync] session {this.GetPrimaryKey().ToString()}, No proxies initialized for region {region}");
        if (region == DefaultRegion)
        {
            Logger.LogWarning($"[GodChatGAgent][GetProxyByRegionAsync] No available proxies for region {region}.");
            return null;
        }

        return await GetProxyByRegionAsync(DefaultRegion);;
    }
    
    private async Task<List<Guid>> InitializeRegionProxiesAsync(string region)
    {
        var llmsForRegion = GetLLMsForRegion(region);
        if (llmsForRegion.IsNullOrEmpty())
        {
            Logger.LogDebug($"[GodChatGAgent][InitializeRegionProxiesAsync] session {this.GetPrimaryKey().ToString()}, initialized proxy for region {region}, LLM not config");
            return new List<Guid>();
        }

        var proxies = new List<Guid>();
        foreach (var llm in llmsForRegion)
        {
            var proxy = GrainFactory.GetGrain<IAIAgentStatusProxy>(Guid.NewGuid());
            await proxy.ConfigAsync(new AIAgentStatusProxyConfig
            {
                Instructions = await GetConfiguration().GetPrompt(),
                LLMConfig = new LLMConfigDto { SystemLLM = llm },
                StreamingModeEnabled = true,
                StreamingConfig = new StreamingConfig { BufferingSize = 32 },
                RequestRecoveryDelay = RequestRecoveryDelay,
                ParentId = this.GetPrimaryKey()
            });

            proxies.Add(proxy.GetPrimaryKey());
            Logger.LogDebug($"[GodChatGAgent][InitializeRegionProxiesAsync] session {this.GetPrimaryKey().ToString()}, initialized proxy for region {region} with LLM {llm}. id {proxy.GetPrimaryKey().ToString()}");
        }

        return proxies;
    }
    
    private List<string> GetLLMsForRegion(string region)
    {
        return RegionToLLMsMap.TryGetValue(region, out var llms) ? llms : new List<string>();
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

        var response = await ChatAsync(message, promptSettings);
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
                contextDto.ChatId, null, (bool)dictionary.GetValueOrDefault("IsHttpRequest", true),
                (string)dictionary.GetValueOrDefault("Region", null));
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
            case UpdateRegionProxiesLogEvent updateRegionProxiesLogEvent:
                foreach (var regionProxy in updateRegionProxiesLogEvent.RegionProxies)
                {
                    if (State.RegionProxies == null)
                    {
                        State.RegionProxies = new Dictionary<string, List<Guid>>();
                    }
                    State.RegionProxies[regionProxy.Key] = regionProxy.Value;
                }
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