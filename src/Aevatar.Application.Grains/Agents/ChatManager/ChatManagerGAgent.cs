using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Options;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[Description("manage chat agent")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(ChatGAgentManager))]
public class ChatGAgentManager : AIGAgentBase<ChatManagerGAgentState, ChatManageEventLog>,
    IChatManagerGAgent
{
    private readonly QuantumOptions _options;

    public ChatGAgentManager(IOptions<QuantumOptions> options)
    {
        _options = options.Value;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat GAgent Manager");
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestCreateQuantumChatEvent @event)
    {
        var sessionId = Guid.Empty;
        try
        {
            sessionId = await CreateSessionAsync(@event.SystemLLM, @event.Prompt);
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[ChatGAgentManager][RequestCreateQuantumChatEvent] handle error:{e.ToString()}");
        }
        await PublishAsync(new ResponseCreateQuantum()
        {
            SessionId = sessionId
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestQuantumChatEvent @event)
    {
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
            Logger.LogError(e, $"[ChatGAgentManager][RequestQuantumChatEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseQuantumChat()
        {
            Response = content,
            NewTitle = title,
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestQuantumSessionListEvent @event)
    {
        var response = await GetSessionListAsync();
        await PublishAsync(new ResponseQuantumSessionList()
        {
            SessionList = response,
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestSessionChatHistoryEvent @event)
    {
        var response = await GetSessionMessageListAsync(@event.SessionId);
        await PublishAsync(new ResponseSessionChatHistory()
        {
            ChatHistory = response
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestDeleteSessionEvent @event)
    {
        await DeleteSessionAsync(@event.SessionId);
        await PublishAsync(new ResponseDeleteSession()
        {
            IfSuccess = true
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestRenameSessionEvent @event)
    {
        await RenameSessionAsync(@event.SessionId, @event.Title);
        await PublishAsync(new ResponseRenameSession()
        {
            SessionId = @event.SessionId,
            Title = @event.Title,
        });
    }

    public async Task<Guid> CreateSessionAsync(string systemLLM, string prompt)
    {
        IQuantumChat quantumChat = GrainFactory.GetGrain<IQuantumChat>(Guid.NewGuid());
        await quantumChat.ConfigAsync(new ChatConfigDto()
        {
            Instructions = prompt, MaxHistoryCount = 32,
            LLMConfig = new LLMConfigDto() { SystemLLM = systemLLM }
        });

        RaiseEvent(new CreateSessionInfoEventLog()
        {
            SessionId = quantumChat.GetPrimaryKey(),
            Title = ""
        });

        return quantumChat.GetPrimaryKey();
    }

    public async Task<Tuple<string, string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null)
    {
        var sessionInfo = State.GetSession(sessionId);
        IQuantumChat quantumChat = GrainFactory.GetGrain<IQuantumChat>(sessionId);
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

        var response = await quantumChat.QuantumChatAsync(sysmLLM, content, promptSettings);
        return new Tuple<string, string>(response, title);
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

        var quantumChat = GrainFactory.GetGrain<IQuantumChat>(sessionInfo.SessionId);
        return await quantumChat.GetChatMessageAsync();
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
        }
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        if (State.SystemLLM != _options.SystemLLM)
        {
            await InitializeAsync(new InitializeDto()
            {
                Instructions = "Please summarize the following content briefly, with no more than 8 words.",
                LLMConfig = new LLMConfigDto() { SystemLLM = _options.SystemLLM }
            });
        }

        await base.OnAIGAgentActivateAsync(cancellationToken);
    }
}