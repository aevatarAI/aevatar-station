using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

[GenerateSerializer]
public class ConfigurationState : StateBase
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
    [Id(2)] public bool StreamingModeEnabled { get; set; }
    [Id(3)] public long DefaultCredits { get; set; } = 100;
    [Id(4)] public string UserProfilePrompt { get; set; } = "I am {Gender}; My birth date is {BirthDate}; My place of birth is {BirthPlace}; Please give me a fortune-telling in English.";
}