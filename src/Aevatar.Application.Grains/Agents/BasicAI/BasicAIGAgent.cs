using Aevatar.Agents.BasicAI;
using Aevatar.AI.Agent;
using Aevatar.AI.State;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.BasicAI;

public interface IBasicAIGAgent : IAIGAgent, IGAgent
{
    Task<string> InvokeLLMAsync(string prompt);
}

public class BasicAIGAgent : AIGAgentBase<BasicAIGAgentState, BasicAIGAgentLogEvent>, IBasicAIGAgent
{
    public BasicAIGAgent(ILogger<BasicAIGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for calling LLM.");
    }
    
    public async Task<string> InvokeLLMAsync(string prompt)
    {
        return await InvokePromptAsync(prompt) ?? string.Empty;
    }
    
}