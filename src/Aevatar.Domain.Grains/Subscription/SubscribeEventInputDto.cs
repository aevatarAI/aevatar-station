namespace Aevatar.Domain.Grains.Subscription;
[GenerateSerializer]
public class SubscribeEventInputDto
{
    [Id(0)] public Guid AgentId { get; set; }
    [Id(1)] public List<string> EventTypes { get; set; }
    [Id(2)] public string CallbackUrl { get; set; }
    [Id(3)] public Guid UserId { get; set; }
}
