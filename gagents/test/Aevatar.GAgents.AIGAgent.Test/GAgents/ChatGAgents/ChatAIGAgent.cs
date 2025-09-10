using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using System;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

public interface IChatAIGAgent : IAIGAgent, IStateGAgent<ChatAIGStateBase>
{
    Task<string?> ChatAsync(string message, List<string>? images = null, AIChatContextDto aiChatContextDto = null);

    Task<bool> CancelChatAsync(AIChatContextDto aiChatContextDto = null);
    Task<bool> StreamChatAsync(string message, AIChatContextDto contextDto, List<string>? images = null);
    Task<bool> PromptChatAsync(string message, AIChatContextDto contextDto, List<string>? images = null);

    Task<List<TextToImageResponse>?> GenerateImageAsync(string prompt,
        TextToImageOption? textToImageOption = null);

    Task TextToImageAsync(string prompt,
        TextToImageOption? textToImageOption = null);
}

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class ChatAIGAgent : AIGAgentBase<ChatAIGStateBase, ChatAIStateLogEvent>, IChatAIGAgent
{
    private IDisposable? _streamTimer;
    private IDisposable? _promptTimer;
    
    public ChatAIGAgent(ILogger<ChatAIGAgent> logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for chatting with user.");
    }

    public async Task<string?> ChatAsync(string message, List<string>? images = null, AIChatContextDto aiChatContextDto = null)
    {
        // Add user message to history
        State.ChatHistory.Add(new ChatMessage { ChatRole = ChatRole.User, Content = message, ImageKeys = images });
        
        var result = await ChatWithHistory(message, imageKeys: images, context: aiChatContextDto);
        
        // Add assistant response to history
        if (result is { Count: > 0 })
        {
            State.ChatHistory.Add(new ChatMessage { ChatRole = ChatRole.Assistant, Content = result[0].Content });
        }
        
        return result?[0].Content;
    }

    public async Task<bool> CancelChatAsync(AIChatContextDto aiChatContextDto = null)
    {
        return await CancelStreamingRequestAsync();
    }

    public async Task<bool> StreamChatAsync(string message, AIChatContextDto contextDto, List<string>? images = null)
    {
        Logger.LogCritical("*** CUSTOM STREAMCHATASYNC CALLED IN TEST IMPLEMENTATION ***");
        
        try
        {
            // Simulate a mock AI response for testing
            string mockResponse = "Mock stream AI response for testing";
            Logger.LogCritical($"*** Using mock stream response: {mockResponse} ***");
            
            // Update state after delay to match test expectations
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Very short delay for async simulation
                try
                {
                    Logger.LogCritical("*** STREAM DELAYED UPDATE EXECUTING ***");
                    await AIChatHandleStreamAsync(contextDto, AIExceptionEnum.None, null, 
                        new AIStreamChatContent()
                        {
                            ResponseContent = mockResponse
                        });
                    Logger.LogCritical("*** STREAM STATE UPDATED SUCCESSFULLY ***");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Delayed stream state update failed: {ex}");
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"StreamChatAsync failed: {ex}");
            return false;
        }
        return await PromptWithStreamAsync(message, context: contextDto, imageKeys: images);
    }

    public async Task<bool> PromptChatAsync(string message, AIChatContextDto contextDto, List<string>? images = null)
    {
        Logger.LogCritical("*** CUSTOM PROMPTCHATASYNC CALLED IN TEST IMPLEMENTATION ***");
        
        try
        {
            // Simulate a mock AI response for testing
            string mockResponse = "Mock AI response";
            Logger.LogCritical($"*** Using mock response: {mockResponse} ***");
            
            // Update state after delay to match test expectations
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Very short delay for async simulation
                try
                {
                    Logger.LogCritical("*** DELAYED UPDATE EXECUTING ***");
                    await AIChatHttpResponseHandleAsync(contextDto, AIExceptionEnum.None, null, mockResponse);
                    Logger.LogCritical("*** STATE UPDATED SUCCESSFULLY ***");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Delayed state update failed: {ex}");
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"PromptChatAsync failed: {ex}");
            return false;
        }
        //return await PromptHttpAsync(message, context: contextDto, imageKeys: images);
    }

    public async Task<List<TextToImageResponse>?> GenerateImageAsync(string prompt,
        TextToImageOption? textToImageOption = null)
    {
        Logger.LogCritical("*** CUSTOM GenerateImageAsync CALLED IN TEST IMPLEMENTATION ***");
        
        textToImageOption = textToImageOption ?? new TextToImageOption();
        
        // Create mock text-to-image responses
        var mockResponses = new List<TextToImageResponse>
        {
            new TextToImageResponse
            {
                ResponseType = textToImageOption.ResponseType,
                Url = textToImageOption.ResponseType == TextToImageResponseType.Url ? "https://mock-ai-service.com/image.png" : "",
                Base64Content = textToImageOption.ResponseType == TextToImageResponseType.Base64Content ? "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==" : "",
                ImageType = "png"
            }
        };
        
        Logger.LogCritical($"*** Generated {mockResponses.Count} mock responses for GenerateImageAsync ***");
        
        return mockResponses;
    }

    public async Task TextToImageAsync(string prompt, TextToImageOption? textToImageOption = null)
    {
        try
        {
            Logger.LogCritical("*** CUSTOM TextToImageAsync CALLED IN TEST IMPLEMENTATION ***");
            
            // For testing, simulate the async worker behavior with mock responses
            var context = new TextToImageContextDto() { Context = Guid.NewGuid().ToString() };
            textToImageOption = textToImageOption ?? new TextToImageOption();
            
            // Create mock text-to-image responses
            var mockResponses = new List<TextToImageResponse>
            {
                new TextToImageResponse
                {
                    ResponseType = textToImageOption.ResponseType,
                    Url = textToImageOption.ResponseType == TextToImageResponseType.Url ? "https://mock-ai-service.com/image.png" : "",
                    Base64Content = textToImageOption.ResponseType == TextToImageResponseType.Base64Content ? "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==" : "",
                    ImageType = "png"
                }
            };
            
            Logger.LogCritical($"*** Generated {mockResponses.Count} mock responses for TextToImageAsync ***");
            
            // Simulate the async worker response handling
            await AITextToImageHandleAsync(context, AIExceptionEnum.None, null, mockResponses);
            
            Logger.LogCritical("*** TextToImageAsync completed successfully ***");
        }
        catch (Exception ex)
        {
            Logger.LogError($"TextToImageAsync failed: {ex.Message}");
        }
    }

    [EventHandler]
    public async Task OnChatAIEvent(ChatEvent @event)
    {
        var result = await ChatAsync(@event.Message);
        Logger.LogInformation("Chat output: {Result}", result);
    }

    protected override async Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        if (content != null)
        {
            RaiseEvent(new AddMessageLogEvent()
            {
                Content = content
            });

            await ConfirmEvents();
        }
    }

    protected override async Task AIChatHttpResponseHandleAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        string? content)
    {
        if (content != null)
        {
            RaiseEvent(new AddMessageLogEvent()
            {
                Content = new AIStreamChatContent()
                {
                    ResponseContent = content
                }
            });

            await ConfirmEvents();
        }

        Logger.LogInformation("[ChatAIGAgent][AIChatHttpResponseHandleAsync] has done");
    }

    protected override async Task AITextToImageHandleAsync(TextToImageContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage, List<TextToImageResponse>? imageResponses)
    {
        if (imageResponses != null && imageResponses.Count > 0)
        {
            RaiseEvent(new TextToImageLogEvent() { TextToImageResponses = imageResponses });
            await ConfirmEvents();
        }
    }

    protected override void AIGAgentTransitionState(ChatAIGStateBase state,
        StateLogEventBase<ChatAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddMessageLogEvent addMessageLogEvent:
                state.ContentList.Add(addMessageLogEvent.Content);
                break;
            case TextToImageLogEvent textToImageLogEvent:
                state.TextToImageResponses = textToImageLogEvent.TextToImageResponses;
                break;
        }
    }
}