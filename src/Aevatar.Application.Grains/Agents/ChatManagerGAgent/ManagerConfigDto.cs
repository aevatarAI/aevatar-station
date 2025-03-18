using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatGAgentManager;

[GenerateSerializer]
public class ManagerConfigDto : ConfigurationBase
{
    [Id(0)] public Guid UserId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public  int MaxSession { get; set; }
}