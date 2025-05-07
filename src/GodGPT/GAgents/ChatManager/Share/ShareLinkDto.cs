using Aevatar.GAgents.AI.Common;

namespace Aevatar.Application.Grains.Agents.ChatManager.Share;

[GenerateSerializer]
public class ShareLinkDto
{
    [Id(0)] public Guid UserId { get; set; }
    [Id(1)] public Guid SessionId { get; set; }
    [Id(2)] public List<ChatMessage> Messages { get; set; }
    [Id(3)] public DateTime CreateTime { get; set; }
} 