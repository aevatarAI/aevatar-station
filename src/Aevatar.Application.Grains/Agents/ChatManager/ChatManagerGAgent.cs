using System.Diagnostics;
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
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat GAgent Manager");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(RequestStreamGodChatEvent @event)
    {
        string chatId = Guid.NewGuid().ToString();
        Logger.LogDebug($"[ChatGAgentManager][RequestStreamGodChatEvent] start:{JsonConvert.SerializeObject(@event)} chatID:{chatId}");
        var title = "";
        var content = "";
        var isLastChunk = false;

        try
        {
            if (State.StreamingModeEnabled)
            {
                Logger.LogDebug("State.StreamingModeEnabled is on");
                await StreamChatWithSessionAsync(@event.SessionId, @event.SystemLLM, @event.Content,chatId);
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
            ChatId =chatId,
            Response = content,
            NewTitle = title,
            IsLastChunk = isLastChunk
        });

        Logger.LogDebug($"[ChatGAgentManager][RequestStreamGodChatEvent] end:{JsonConvert.SerializeObject(@event)}");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(AIStreamingErrorResponseGEvent @event)
    {
        Logger.LogDebug($"[ChatGAgentManager][AIStreamingErrorResponseGEvent] start:{JsonConvert.SerializeObject(@event)}");

        await PublishAsync(new ResponseStreamGodChat()
        {
            Response = "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak.",
            ChatId = @event.Context.ChatId,
            IsLastChunk = true,
            SerialNumber = -2
        });
        
        Logger.LogDebug($"[ChatGAgentManager][AIStreamingErrorResponseGEvent] end:{JsonConvert.SerializeObject(@event)}");

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
        var sessionId = Guid.Empty;
        try
        {
            sessionId = await CreateSessionAsync(@event.SystemLLM, @event.Prompt);
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
            "[ChatGAgentManager][RequestCreateGodChatEvent] sessionId:{A} end {B} ", sessionId,
            JsonConvert.SerializeObject(@event));
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestGodChatEvent @event)
    {
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

    public async Task<Guid> CreateSessionAsync(string systemLLM, string prompt)
    {
        var configuration = GetConfiguration();
        Stopwatch sw = new Stopwatch();
        sw.Start();
        IGodChat godChat = GrainFactory.GetGrain<IGodChat>(Guid.NewGuid());
        await RegisterAsync(godChat);
        sw.Stop();
        Logger.LogDebug($"CreateSessionAsync - step,time use:{sw.ElapsedMilliseconds}");
        
        var sysMessage = await configuration.GetPrompt();
        var formattedRequirement =
            """
            
            ### 要求：
            1. 内容部分将所有 Markdown 元素（如标题、加粗、列表等）转换为有效的 HTML 标签。
            2. 不需要返回HTML整个页面，只需要内容部分。
            3. 返回的内容不需要转换为有效的 HTML 格式的提示词。
            4. 内容不需要用 ```html ``` 包裹。
            5. 如果内容里面有公式，需要能够被 react-native-mathjax 解析渲染。
            6. LaTeX 公式用以下字符包裹返回:
              - 行内公式使用 `@@@...===` 包裹返回。
              - 块级公式使用 `aelfstart...aelfend` 包裹返回。
            """;
        
       
        sysMessage += formattedRequirement;
        // Add a new field for the current date and time
        string currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");  
        var currentRequirement = $"\n当前 UTC 时间：{currentTime}, " +
                                 $"\n 回答有关时间的问题时,以UTC时间为基准 ";
        
        sysMessage += currentRequirement;
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

        RaiseEvent(new CreateSessionInfoEventLog()
        {
            SessionId = godChat.GetPrimaryKey(),
            Title = ""
        });

        await ConfirmEvents();

        return godChat.GetPrimaryKey();
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
    
    private async Task StreamChatWithSessionAsync(Guid sessionId, string sysmLLM, string content,string chatId,
        ExecutionPromptSettings promptSettings = null)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var sessionInfo = State.GetSession(sessionId);
        IGodChat godChat = GrainFactory.GetGrain<IGodChat>(sessionId);
        sw.Stop();
        Logger.LogDebug($"StreamChatWithSessionAsync - step1,time use:{sw.ElapsedMilliseconds}");

        var title = "";
        if (sessionInfo == null)
        {
            Logger.LogError("StreamChatWithSessionAsync sessionInfoIsNull sessionId={A}",sessionId);
            return ;
        }
        if (sessionInfo.Title.IsNullOrEmpty())
        {
            sw.Reset();
            sw.Start();
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
            sw.Stop();
            Logger.LogDebug($"StreamChatWithSessionAsync - step3,time use:{sw.ElapsedMilliseconds}");
        }

        sw.Reset();
        sw.Start();
        var configuration = GetConfiguration();
        godChat.GodStreamChatAsync(await configuration.GetSystemLLM(), await configuration.GetStreamingModeEnabled(),content, chatId,promptSettings);
        sw.Stop();
        Logger.LogDebug($"StreamChatWithSessionAsync - step4,time use:{sw.ElapsedMilliseconds}");
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