namespace Aevatar.Application.Grains.Agents.ChatGAgentManager;


[GenerateSerializer]
public class SessionInfo
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}