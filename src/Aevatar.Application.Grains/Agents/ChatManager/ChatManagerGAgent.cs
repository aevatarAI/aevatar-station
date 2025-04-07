using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.Sender;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[Description("manage chat agent")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(ChatGAgentManager))]
public class ChatGAgentManager : AIGAgentBase<ChatManagerGAgentState, ChatManageEventLog>,
    IChatManagerGAgent
{
    private const string FormattedDate = "yyyy-MM-dd";
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat GAgent Manager");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestStreamGodChatEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][RequestStreamGodChatEvent] start:{JsonConvert.SerializeObject(@event)}");
        
        if (!await CheckCreditsAndExecuteAsync(State.UserId, 1, async () =>
            {
                await PublishAsync(new ResponseStreamGodChat()
                {
                    Code = (int) ResponseCode.UserCreditsLow
                });
            }))
        {
            Logger.LogDebug("[ChatGAgentManager][RequestCreateGodChatEvent]userId={0}, check credits failed.", State.UserId);
            return;
        }
        
        var title = "";
        var content = "";
        var isLastChunk = false;
        string chatId = Guid.NewGuid().ToString();

        try
        {
            if (State.StreamingModeEnabled)
            {
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
            Logger.LogError(e, $"[ChatGAgentManager][RequestStreamGodChatEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseStreamGodChat()
        {
            ChatId = chatId,
            Response = content,
            NewTitle = title,
            IsLastChunk = isLastChunk
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestStreamGodChatEvent] end:{JsonConvert.SerializeObject(@event)}");
    }


    [EventHandler]
    public async Task HandleEventAsync(AIStreamingResponseGEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][AIStreamingResponseGEvent] start:{JsonConvert.SerializeObject(@event)}");

        await PublishAsync(new ResponseStreamGodChat()
        {
            Response = @event.ResponseContent,
            ChatId = @event.Context.ChatId,
            IsLastChunk = @event.IsLastChunk,
            SerialNumber = @event.SerialNumber
        });

        Logger.LogDebug($"[ChatGAgentManager][AIStreamingResponseGEvent] end:{JsonConvert.SerializeObject(@event)}");
    }


    [EventHandler]
    public async Task HandleEventAsync(RequestCreateGodChatEvent @event)
    {
        Logger.LogDebug(
            $"[ChatGAgentManager][RequestCreateGodChatEvent] start:{JsonConvert.SerializeObject(@event)}");

        if (!await CheckCreditsAndExecuteAsync(State.UserId, 0, async () =>
            {
                await PublishAsync(new ResponseCreateGod()
                {
                    Code = (int) ResponseCode.UserCreditsLow
                });
            }))
        {
            Logger.LogDebug("[ChatGAgentManager][RequestCreateGodChatEvent]userId={0}, check credits failed.", State.UserId);
            return;
        }

        var sessionId = Guid.Empty;
        try
        {
            sessionId = await CreateSessionAsync(@event.SystemLLM, @event.Prompt, @event.UserProfile);
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[ChatGAgentManager][RequestCreateGodChatEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseCreateGod()
        {
            SessionId = sessionId
        });

        Logger.LogDebug(
            $"[ChatGAgentManager][RequestCreateGodChatEvent] end :{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestGodChatEvent @event)
    {
        
        if (!await CheckCreditsAndExecuteAsync(State.UserId, 1, async () =>
            {
                await PublishAsync(new ResponseGodChat()
                {
                    Code = (int)ResponseCode.UserCreditsLow
                });
            }))
        {
            Logger.LogDebug("[ChatGAgentManager][RequestGodChatEvent]userId={0}, check credits failed.", State.UserId);
            return;
        }

        Logger.LogDebug($"[ChatGAgentManager][RequestGodChatEvent] start:{JsonConvert.SerializeObject(@event)}");
        var title = "";
        var content = "";
        try
        {
            var response = await ChatWithSessionAsync(@event.SessionId, @event.SystemLLM, @event.Content);
            content = response.Item1;
            title = response.Item2;
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[ChatGAgentManager][RequestGodChatEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseGodChat()
        {
            Response = content,
            NewTitle = title,
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestGodChatEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestGodSessionListEvent @event)
    {
        Logger.LogDebug(
            $"[ChatGAgentManager][RequestGodSessionListEvent] start:{JsonConvert.SerializeObject(@event)}");
        var response = await GetSessionListAsync();
        await PublishAsync(new ResponseGodSessionList()
        {
            SessionList = response,
        });

        Logger.LogDebug(
            $"[ChatGAgentManager][RequestGodSessionListEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestSessionChatHistoryEvent @event)
    {
        Logger.LogDebug(
            $"[ChatGAgentManager][RequestSessionChatHistoryEvent] start:{JsonConvert.SerializeObject(@event)}");
        var response = await GetSessionMessageListAsync(@event.SessionId);
        await PublishAsync(new ResponseSessionChatHistory()
        {
            ChatHistory = response
        });

        Logger.LogDebug(
            $"[ChatGAgentManager][RequestSessionChatHistoryEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestDeleteSessionEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][RequestDeleteSessionEvent] start:{JsonConvert.SerializeObject(@event)}");
        await DeleteSessionAsync(@event.SessionId);
        await PublishAsync(new ResponseDeleteSession()
        {
            IfSuccess = true
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestDeleteSessionEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestRenameSessionEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][RequestRenameSessionEvent] start:{JsonConvert.SerializeObject(@event)}");
        await RenameSessionAsync(@event.SessionId, @event.Title);
        await PublishAsync(new ResponseRenameSession()
        {
            SessionId = @event.SessionId,
            Title = @event.Title,
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestRenameSessionEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestClearAllEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][RequestClearAllEvent] start:{JsonConvert.SerializeObject(@event)}");

        bool success = false;
        try
        {
            await ClearAllAsync();
            success = true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[ChatGAgentManager][RequestClearAllEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseClearAll()
        {
            Success = success
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestClearAllEvent] end:{JsonConvert.SerializeObject(@event)}");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestSetUserProfileEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][RequestSetFortuneInfoEvent] start:{JsonConvert.SerializeObject(@event)}");

        bool success = false;
        try
        {
            await SetUserProfileAsync(@event.Gender, @event.BirthDate, @event.BirthPlace);
            success = true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[ChatGAgentManager][RequestSetFortuneInfoEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseSetUserProfile()
        {
            Success = success
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestSetFortuneInfoEvent] end");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestGetUserProfileEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][RequestGetUserProfileEvent] start");

        var userProfileDto = await GetUserProfileAsync();
        var userCredits = await UpdateAndGetUserCreditsAsync(0);

        await PublishAsync(new ResponseGetUserProfile()
        {
            Gender = userProfileDto.Gender,
            BirthDate = userProfileDto.BirthDate,
            BirthPlace = userProfileDto.BirthPlace,
            Credits = userCredits
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestGetUserProfileEvent] end");
    }
    
    public async Task<bool> CheckCreditsAndExecuteAsync(Guid userId, int requestCount, Action onCreditsLowResponse)
    {
        var credits = await UpdateAndGetUserCreditsAsync(requestCount);
        Logger.LogDebug("[ChatGAgentManager] userId={0},credits={1}", userId, credits);

        if (credits <= 0)
        {
            onCreditsLowResponse();
            return false;
        }

        return true;
    }

    private async Task<decimal> UpdateAndGetUserCreditsAsync(int requestCount)
    {
        if (State.Credits.LastUpdated.Date != DateTime.UtcNow.Date)
        {
            var configuration = GetConfiguration();
            var defaultCredits = await configuration.GetDefaultCreditsAsync();

            RaiseEvent(new SetUserCreditsLogEvent
            {
                Value = defaultCredits,
                Consumed = State.Credits.Consumed,
                LastUpdated = DateTime.UtcNow
            });

            await ConfirmEvents();
        }

        var value = State.Credits.Value;
        if (requestCount > 0)
        {
            var consumed = State.Credits.Consumed * requestCount;
            value = value - consumed;
            if (value >= 0)
            {
                RaiseEvent(new SetUserCreditsLogEvent
                {
                    Value = value,
                    Consumed = State.Credits.Consumed,
                    LastUpdated = DateTime.UtcNow
                });

                await ConfirmEvents();
            }
        }

        return value;
    }

    public async Task<Guid> CreateSessionAsync(string systemLLM, string prompt, UserProfileDto? userProfile = null)
    {
        var configuration = GetConfiguration();
        IGodChat godChat = GrainFactory.GetGrain<IGodChat>(Guid.NewGuid());
        var sysMessage = await configuration.GetPrompt();
        sysMessage = await AppendUserInfoToSystemPromptAsync(configuration, sysMessage, userProfile);
        Logger.LogDebug("Retrieved system prompt from configuration: {SysMessage}", sysMessage);
        await godChat.ConfigAsync(new ChatConfigDto()
        {
            Instructions = sysMessage, MaxHistoryCount = 32,
            LLMConfig = new LLMConfigDto() { SystemLLM = await configuration.GetSystemLLM() },
            StreamingModeEnabled = true, StreamingConfig = new StreamingConfig()
            {
                BufferingSize = 32
            }
        });
        if (userProfile != null)
        {
            await godChat.SetUserProfileAsync(userProfile);
        }

        RaiseEvent(new CreateSessionInfoEventLog()
        {
            SessionId = godChat.GetPrimaryKey(),
            Title = ""
        });

        await ConfirmEvents();

        return godChat.GetPrimaryKey();
    }

    private async Task<string> AppendUserInfoToSystemPromptAsync(IConfigurationGAgent configurationGAgent,
        string sysMessage,
        UserProfileDto? userProfile)
    {
        if (userProfile == null)
        {
            return sysMessage;
        }

        var userProfilePrompt = await configurationGAgent.GetUserProfilePromptAsync();
        if (userProfilePrompt.IsNullOrWhiteSpace())
        {
            return sysMessage;
        }
        
        var variables = new Dictionary<string, string>
        {
            { "Gender", userProfile.Gender },
            { "BirthDate", userProfile.BirthDate.ToString(FormattedDate) },
            { "BirthPlace", userProfile.BirthPlace }
        };

        userProfilePrompt = variables.Aggregate(userProfilePrompt,
            (current, pair) => current.Replace("{" + pair.Key + "}", pair.Value));

        return $"{sysMessage} \n {userProfilePrompt}";
    }

    public async Task<Tuple<string, string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null)
    {
        var sessionInfo = State.GetSession(sessionId);
        IGodChat godChat = GrainFactory.GetGrain<IGodChat>(sessionId);

        if (sessionInfo == null)
        {
            return new Tuple<string, string>("", "");
        }

        var title = "";
        if (sessionInfo.Title.IsNullOrEmpty())
        {
            var titleList = await ChatWithHistory(content);
            title = titleList is { Count: > 0 }
                ? titleList[0].Content!
                : string.Join(" ", content.Split(" ").Take(4));

            RaiseEvent(new RenameTitleEventLog()
            {
                SessionId = sessionId,
                Title = title
            });

            await ConfirmEvents();
        }

        var configuration = GetConfiguration();
        var response = await godChat.GodChatAsync(await configuration.GetSystemLLM(), content, promptSettings);
        return new Tuple<string, string>(response, title);
    }

    private async Task StreamChatWithSessionAsync(Guid sessionId, string sysmLLM, string content, string chatId,
        ExecutionPromptSettings promptSettings = null)
    {
        var sessionInfo = State.GetSession(sessionId);
        IGodChat godChat = GrainFactory.GetGrain<IGodChat>(sessionId);

        await RegisterAsync(godChat);


        var title = "";
        if (sessionInfo == null)
        {
            return;
        }

        if (sessionInfo.Title.IsNullOrEmpty())
        {
            var titleList = await ChatWithHistory(content);
            title = titleList is { Count: > 0 }
                ? titleList[0].Content!
                : string.Join(" ", content.Split(" ").Take(4));

            RaiseEvent(new RenameTitleEventLog()
            {
                SessionId = sessionId,
                Title = title
            });

            await ConfirmEvents();
        }

        var configuration = GetConfiguration();
        await godChat.GodStreamChatAsync(await configuration.GetSystemLLM(),
            await configuration.GetStreamingModeEnabled(), content, chatId, promptSettings);
    }

    public Task<List<SessionInfoDto>> GetSessionListAsync()
    {
        var result = new List<SessionInfoDto>();
        foreach (var item in State.SessionInfoList)
        {
            result.Add(new SessionInfoDto()
            {
                SessionId = item.SessionId,
                Title = item.Title,
            });
        }

        return Task.FromResult(result);
    }

    public async Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId)
    {
        var sessionInfo = State.GetSession(sessionId);
        if (sessionInfo == null)
        {
            return new List<ChatMessage>();
        }

        var godChat = GrainFactory.GetGrain<IGodChat>(sessionInfo.SessionId);
        return await godChat.GetChatMessageAsync();
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        if (State.GetSession(sessionId) == null)
        {
            return;
        }

        RaiseEvent(new DeleteSessionEventLog()
        {
            SessionId = sessionId
        });

        await ConfirmEvents();
    }

    public async Task RenameSessionAsync(Guid sessionId, string title)
    {
        var sessionInfo = State.GetSession(sessionId);
        if (sessionInfo == null || sessionInfo.Title == title)
        {
            return;
        }

        RaiseEvent(new RenameTitleEventLog()
        {
            SessionId = sessionId,
            Title = title,
        });

        await ConfirmEvents();
    }

    public async Task ClearAllAsync()
    {
        // Record the event to clear all sessions
        RaiseEvent(new ClearAllEventLog());
        await ConfirmEvents();
    }

    public async Task SetUserProfileAsync(string gender, DateTime birthDate, string birthPlace)
    {
        RaiseEvent(new SetUserProfileEventLog()
        {
            Gender = gender,
            BirthDate = birthDate,
            BirthPlace = birthPlace
        });

        await ConfirmEvents();
    }

    public async Task<UserProfileDto> GetUserProfileAsync()
    {
        var sessionInfo = State.SessionInfoList.LastOrDefault(new SessionInfo());
        if (sessionInfo.SessionId == Guid.Empty)
        {
            return new UserProfileDto();
        }
        
        var godChat = GrainFactory.GetGrain<IGodChat>(sessionInfo.SessionId);
        var userProfileDto = await godChat.GetUserProfileAsync();
        return userProfileDto ?? new UserProfileDto();
    }

    protected override void AIGAgentTransitionState(ChatManagerGAgentState state,
        StateLogEventBase<ChatManageEventLog> @event)
    {
        switch (@event)
        {
            case CreateSessionInfoEventLog @createSessionInfo:
                State.SessionInfoList.Add(new SessionInfo()
                {
                    SessionId = @createSessionInfo.SessionId,
                    Title = @createSessionInfo.Title
                });
                break;
            case DeleteSessionEventLog @deleteSessionEventLog:
                State.SessionInfoList.RemoveAll(f => f.SessionId == @deleteSessionEventLog.SessionId);
                break;
            case RenameTitleEventLog @renameTitleEventLog:
                var sessionInfo = State.SessionInfoList.First(f => f.SessionId == @renameTitleEventLog.SessionId);
                sessionInfo.Title = renameTitleEventLog.Title;
                break;
            case ClearAllEventLog:
                State.SessionInfoList.Clear();
                break;
            case SetUserProfileEventLog @setFortuneInfoEventLog:
                State.Gender = @setFortuneInfoEventLog.Gender;
                State.BirthDate = @setFortuneInfoEventLog.BirthDate;
                State.BirthPlace = @setFortuneInfoEventLog.BirthPlace;
                break;
            case SetUserCreditsLogEvent @setUserCreditsLogEvent:
                state.Credits.Value = @setUserCreditsLogEvent.Value;
                state.Credits.Consumed = @setUserCreditsLogEvent.Consumed;
                state.Credits.LastUpdated = @setUserCreditsLogEvent.LastUpdated;
                break;

        }
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();

        var llm = await configuration.GetSystemLLM();
        var streamingModeEnabled = await configuration.GetStreamingModeEnabled();
        if (State.SystemLLM != llm || State.StreamingModeEnabled != streamingModeEnabled)
        {
            await InitializeAsync(new InitializeDto()
            {
                Instructions = "Please summarize the following content briefly, with no more than 8 words.",
                LLMConfig = new LLMConfigDto() { SystemLLM = await configuration.GetSystemLLM(), },
                StreamingModeEnabled = await configuration.GetStreamingModeEnabled()
            });
        }

        await base.OnAIGAgentActivateAsync(cancellationToken);
    }

    private IConfigurationGAgent GetConfiguration()
    {
        return GrainFactory.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
    }
}