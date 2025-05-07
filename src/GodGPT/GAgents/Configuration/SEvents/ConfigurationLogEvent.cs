using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

[GenerateSerializer]
public class ConfigurationLogEvent : StateLogEventBase<ConfigurationLogEvent>
{
}

[GenerateSerializer]
public class InitLogEvent : ConfigurationLogEvent
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
}

[GenerateSerializer]
public class SetSystemLLMLogEvent : ConfigurationLogEvent
{
    [Id(0)] public string SystemLLM { get; set; }
}

[GenerateSerializer]
public class SetPromptLogEvent : ConfigurationLogEvent
{
    [Id(0)] public string Prompt { get; set; }
}

[GenerateSerializer]
public class SetStreamingModeEnabledLogEvent : ConfigurationLogEvent
{
    [Id(0)] public bool StreamingModeEnabled { get; set; }
}

[GenerateSerializer]
public class SetUserProfilePromptLogEvent : ConfigurationLogEvent
{
    [Id(0)] public string UserProfilePrompt { get; set; }
}