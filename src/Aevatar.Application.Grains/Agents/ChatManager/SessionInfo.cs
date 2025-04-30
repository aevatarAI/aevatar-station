namespace Aevatar.Application.Grains.Agents.ChatManager;


[GenerateSerializer]
public class SessionInfo
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
    [Id(2)] public DateTime CreateAt { get; set; }
    [Id(3)] public List<Guid> ShareIds { get; set; }
}