using System;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AI.BrainFactory;

public interface IBrainFactory
{
    IBrain? CreateBrain(LLMProviderConfig llmProviderConfig);
    IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig);
    ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig);
}