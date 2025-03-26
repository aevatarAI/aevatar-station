using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Json.Schema.Generation;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[Description("god chat agent")]
public class GodChatGAgent : ChatGAgentBase<GodChatState, GodChatEventLog, EventBase, ChatConfigDto>, IGodChat
{
    public async Task<string> GodChatAsync(string llm, string message,
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

    public async Task<string> GodStreamChatAsync(string llm, bool streamingModeEnabled,string message, String chatId,
        ExecutionPromptSettings? promptSettings = null)
    {
        if (State.SystemLLM != llm || State.StreamingModeEnabled != streamingModeEnabled)
        {
            await InitializeAsync(new InitializeDto()
            {
                Instructions = State.PromptTemplate, LLMConfig = new LLMConfigDto() { SystemLLM = llm },
                StreamingModeEnabled = true, StreamingConfig = new StreamingConfig()
                {
                    BufferingSize = 32
                }
            });
        }

        AIChatContextDto aiChatContextDto = new AIChatContextDto() { ChatId = chatId };
        await ChatAsync(message, promptSettings,aiChatContextDto);

        return string.Empty;
    }

    public Task<List<ChatMessage>> GetChatMessageAsync()
    {
        return Task.FromResult(State.ChatHistory);
    }
}