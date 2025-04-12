using Aevatar.GAgents.ChatAgent.GAgent.State;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[GenerateSerializer]
public class GodChatState:ChatGAgentState
{
    [Id(0)] public UserProfile? UserProfile { get; set; }
}

[GenerateSerializer]
public class UserProfile
{
    [Id(0)] public string Gender { get; set; }
    [Id(1)] public DateTime BirthDate { get; set; }
    [Id(2)] public string BirthPlace { get; set; }
}