using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

public class QuantumChatGAgent : ChatGAgentBase<QuantumChatState, QuantumChatEventLog, EventBase, ChatConfigDto>, IQuantumChat
{
    public async Task<string> QuantumChatAsync(string llm, string message,
        ExecutionPromptSettings? promptSettings = null)
    {
        if (State.SystemLLM != llm)
        {
            await InitializeAsync(new InitializeDto()
                { Instructions = State.PromptTemplate, LLMConfig = new LLMConfigDto() { SystemLLM = llm } });
        }
        
        var response = await ChatAsync(message, promptSettings);
        if (response is { Count: > 0 })
        {
            return response[0].Content!;
        }

        return string.Empty;
    }

    public Task<List<ChatMessage>> GetChatMessageAsync()
    {
        return Task.FromResult(State.ChatHistory);
    }
}