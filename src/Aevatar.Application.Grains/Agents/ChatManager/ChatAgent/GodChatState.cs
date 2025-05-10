using Aevatar.GAgents.ChatAgent.GAgent.State;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[GenerateSerializer]
public class GodChatState:ChatGAgentState
{
    [Id(0)] public UserProfile? UserProfile { get; set; }
    [Id(1)] public string? Title { get; set; }
    [Id(2)] public Guid ChatManagerGuid { get; set; }
    [Obsolete("This class is deprecated and no longer in use.")]
    [Id(3)] public List<Guid> AIAgentIds { get; set; } = new List<Guid>();
    [Id(4)] public Dictionary<string, List<Guid>> RegionProxies = new ();
}

[GenerateSerializer]
public class UserProfile
{
    [Id(0)] public string Gender { get; set; }
    [Id(1)] public DateTime BirthDate { get; set; }
    [Id(2)] public string BirthPlace { get; set; }
    [Id(3)] public string FullName { get; set; }
}