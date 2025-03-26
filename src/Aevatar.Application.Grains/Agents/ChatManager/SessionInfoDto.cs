
namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class SessionInfoDto
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}
