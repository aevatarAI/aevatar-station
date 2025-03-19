using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class ManagerConfigDto : ConfigurationBase
{
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public  int MaxSession { get; set; }
}