using Aevatar.Application.Grains.Agents.ChatGAgentManager.ChatAgent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Json.Schema.Generation;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.ChatGAgentManager;

[Description("manage chat agent")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class ChatGAgentManager : AIGAgentBase<ChatManagerGAgentState, ChatManageEventLog, EventBase, ManagerConfigDto>,
    IChatManagerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat GAgent Manager");
    }

    public async Task<string> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null, CancellationToken cancellationToken = default)
    {
        var sessionInfo = State.GetSession(sessionId);
        IQuantumChat quantumChat = GrainFactory.GetGrain<IQuantumChat>(sessionId);
        if (sessionInfo == null)
        {
            await quantumChat.ConfigAsync(new ChatConfigDto()
            {
                Instructions = "You are an intelligent robot. Please answer the user's questions", MaxHistoryCount = 32,
                LLMConfig = new LLMConfigDto() { }
            });

            var titleList = await ChatWithHistory(content, cancellationToken: cancellationToken);
            var title = titleList is { Count: > 0 }
                ? titleList[0].Content!
                : string.Join(" ", content.Split(" ").Take(4));

            RaiseEvent(new CreateSessionInfoEventLog()
            {
                SessionId = sessionId,
                Title = title
            });

            await ConfirmEvents();
        }

        return await quantumChat.QuantumChatAsync(sysmLLM, content, promptSettings);
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
            case CreateManagerEventLog @createManagerEventLog:
                State.UserId = @createManagerEventLog.UserId;
                State.MaxSession = @createManagerEventLog.MaxSession;
                break;
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

    protected override async Task PerformConfigAsync(ManagerConfigDto configuration)
    {
        RaiseEvent(new CreateManagerEventLog()
        {
            UserId = configuration.UserId,
            MaxSession = configuration.MaxSession,
        });

        await InitializeAsync(new InitializeDto()
        {
            Instructions = "Please summarize the following content briefly, with no more than 8 words.",
            LLMConfig = new LLMConfigDto() { SystemLLM = configuration.SystemLLM }
        });

        await ConfirmEvents();
    }
}