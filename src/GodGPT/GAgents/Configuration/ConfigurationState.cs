using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

[GenerateSerializer]
public class ConfigurationState : StateBase
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
    [Id(2)] public bool StreamingModeEnabled { get; set; }
    [Id(3)] public string UserProfilePrompt { get; set; } = @"
                I'm {Gender} 
                My Birth date is {BirthDate} and my birth place is {BirthPlace} 
                Please tell me my fate. 
                Remember: respond in the same language the user used when filling in the location. 
            ";
}