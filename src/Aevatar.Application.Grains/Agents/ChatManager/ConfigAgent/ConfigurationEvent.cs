using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

[GenerateSerializer]
public class SetLLMEvent : EventBase
{
    [Id(0)] public string LLM { get; set; }
}


[GenerateSerializer]
public class SetPromptEvent : EventBase
{
    [Id(0)] public string Prompt { get; set; }
}

[GenerateSerializer]
public class SetStreamingModeEnabledEvent : EventBase
{
    [Id(0)] public bool StreamingModeEnabled { get; set; }
}