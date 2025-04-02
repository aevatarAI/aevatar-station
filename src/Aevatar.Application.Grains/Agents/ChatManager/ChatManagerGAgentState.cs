using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class ChatManagerGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<SessionInfo> SessionInfoList { get; set; } = new List<SessionInfo>();
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public int MaxSession { get; set; }
    [Id(3)] public string Gender { get; set; }
    [Id(4)] public DateTime BirthDate { get; set; }
    [Id(5)] public string BirthPlace { get; set; }
    [Id(6)] public UserCredits Credits { get; set; } = new UserCredits();

    public SessionInfo? GetSession(Guid sessionId)
    {
        return SessionInfoList.FirstOrDefault(f=>f.SessionId == sessionId);
    }
}

[GenerateSerializer]
public class UserCredits
{
    [Id(0)] public decimal Value { get; set; }
    [Id(1)] public decimal Consumed { get; set; } = 10;
    [Id(2)] public DateTime LastUpdated { get; set; }
}