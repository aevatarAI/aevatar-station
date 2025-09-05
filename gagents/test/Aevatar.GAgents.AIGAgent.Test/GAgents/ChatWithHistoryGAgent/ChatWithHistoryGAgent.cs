using Aevatar.Core.Abstractions;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatWithHistoryGAgent;

public interface IChatWithHistoryGAgent : IChatAgent, IStateGAgent<ChatWithHistoryState>
{
    // Re-declare to override the base interface method
    new Task<bool> ChatWithStreamAsync(string message, AIChatContextDto aiChatContextDto, ExecutionPromptSettings? promptSettings = null);
}

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class ChatWithHistoryGAgent :
    ChatGAgentBase<ChatWithHistoryState, ChatWithHistoryLogEvent, EventBase, ChatConfigDto>, IChatWithHistoryGAgent
{
    private IDisposable? _chatTimer;
    
    // Override to provide async chat behavior for tests
    public new async Task<bool> ChatWithStreamAsync(string message, AIChatContextDto context, ExecutionPromptSettings? promptSettings = null)
    {
        try
        {
            Logger.LogCritical("*** CUSTOM ChatWithStreamAsync METHOD CALLED ***");
            
            // First add the user message to chat history immediately
            var userMessage = new List<Aevatar.GAgents.AI.Common.ChatMessage>
            {
                new Aevatar.GAgents.AI.Common.ChatMessage() { ChatRole = Aevatar.GAgents.AI.Common.ChatRole.User, Content = message }
            };
            RaiseEvent(new AddChatHistoryLogEvent() { ChatList = userMessage });
            await ConfirmEvents();

            // Simulate AI response after delay
            string mockAIResponse = "Mock AI response for chat history testing";
            Logger.LogCritical($"*** Using mock AI response: {mockAIResponse} ***");
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Very short delay for async simulation
                try
                {
                    Logger.LogCritical("*** CHAT HISTORY DELAYED UPDATE EXECUTING ***");
                    // Add the AI response to chat history
                    var aiMessage = new List<Aevatar.GAgents.AI.Common.ChatMessage>
                    {
                        new Aevatar.GAgents.AI.Common.ChatMessage() { ChatRole = Aevatar.GAgents.AI.Common.ChatRole.Assistant, Content = mockAIResponse }
                    };
                    RaiseEvent(new AddChatHistoryLogEvent() { ChatList = aiMessage });
                    await ConfirmEvents();
                    
                    Logger.LogCritical($"*** CHAT HISTORY STATE UPDATED SUCCESSFULLY ***");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Delayed chat history update failed: {ex}");
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"ChatWithStreamAsync failed: {ex.Message}");
            return false;
        }
    }
}