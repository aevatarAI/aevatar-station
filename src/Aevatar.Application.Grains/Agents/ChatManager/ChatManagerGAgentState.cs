using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class ChatManagerGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<SessionInfo> SessionInfoList { get; set; } = new List<SessionInfo>();
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public int MaxSession { get; set; }

    public SessionInfo? GetSession(Guid sessionId)
    {
        return SessionInfoList.FirstOrDefault(f=>f.SessionId == sessionId);
    }
}