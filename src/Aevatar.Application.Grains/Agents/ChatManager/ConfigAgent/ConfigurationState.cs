using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

[GenerateSerializer]
public class ConfigurationState : StateBase
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
    [Id(2)] public bool StreamingModeEnabled { get; set; }
}