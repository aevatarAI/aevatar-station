// ABOUTME: This file implements a mock IChatBrain for unit testing
// ABOUTME: Provides configurable responses without real AI service calls

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockChatBrain : IChatBrain, ITextToImageBrain
{
    // Shared state across all instances
    private static readonly ConcurrentDictionary<string, InvokePromptResponse> _sharedResponses = new();
    private static readonly ConcurrentDictionary<string, Queue<string>> _sharedStreamingResponses = new();
    
    private InvokePromptResponse? _nextResponse;
    private readonly Queue<string> _streamingResponses = new();
    private string _brainKey = "default"; // Key to identify this brain configuration

    public LLMProviderEnum ProviderEnum { get; private set; }
    public ModelIdEnum ModelIdEnum { get; private set; }

    public Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        ProviderEnum = llmConfig.ProviderEnum;
        ModelIdEnum = llmConfig.ModelIdEnum;
        
        // Create a unique key for this brain configuration
        _brainKey = $"{llmConfig.ProviderEnum}_{llmConfig.ModelIdEnum}";
        
        return Task.CompletedTask;
    }

    public Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null)
    {
        return Task.FromResult(true);
    }

    public Task<InvokePromptResponse?> InvokePromptAsync(string content, List<string>? imageKeys = null, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        // Check shared state first, then instance state, then default
        InvokePromptResponse? response = null;
        
        // Try to get from shared state
        if (_sharedResponses.TryRemove(_brainKey, out var sharedResponse))
        {
            response = sharedResponse;
        }
        // Fall back to instance state
        else if (_nextResponse != null)
        {
            response = _nextResponse;
            _nextResponse = null; // Reset after use
        }
        // Default response
        else
        {
            response = CreateDefaultResponse();
        }
        
        return Task.FromResult<InvokePromptResponse?>(response);
    }

    public Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, List<string>? imageKeys = null, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        string[] responses;
        
        // Check shared streaming responses first
        if (_sharedStreamingResponses.TryRemove(_brainKey, out var sharedQueue) && sharedQueue.Count > 0)
        {
            responses = sharedQueue.ToArray();
        }
        // Fall back to instance responses
        else if (_streamingResponses.Count > 0)
        {
            responses = _streamingResponses.ToArray();
        }
        // Default streaming responses - single chunk to simplify aggregation
        else
        {
            responses = new[] { "Mock streaming response" };
        }

        return Task.FromResult(CreateStreamingResponse(responses));
    }

    public TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
    {
        return new TokenUsageStatistics
        {
            InputToken = 5,
            OutputToken = messageList.Count * 2,
            TotalUsageToken = 5 + (messageList.Count * 2),
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    public void SetNextResponse(InvokePromptResponse response)
    {
        // Store in shared state so it persists across instances
        _sharedResponses.AddOrUpdate(_brainKey, response, (key, oldValue) => response);
        
        // Also set instance state for backward compatibility
        _nextResponse = response;
    }

    public void SetStreamingResponses(params string[] responses)
    {
        // Create a new queue for shared state
        var sharedQueue = new Queue<string>();
        foreach (var response in responses)
        {
            sharedQueue.Enqueue(response);
        }
        _sharedStreamingResponses.AddOrUpdate(_brainKey, sharedQueue, (key, oldValue) => sharedQueue);
        
        // Also set instance state for backward compatibility
        _streamingResponses.Clear();
        foreach (var response in responses)
        {
            _streamingResponses.Enqueue(response);
        }
    }
    
    // Public method to set responses by configuration (for tests that can't get the brain instance)
    public static void SetNextResponseForConfig(LLMProviderEnum providerEnum, ModelIdEnum modelIdEnum, InvokePromptResponse response)
    {
        var key = $"{providerEnum}_{modelIdEnum}";
        _sharedResponses.AddOrUpdate(key, response, (k, oldValue) => response);
    }
    
    // Public method to clear all shared state (for test cleanup)
    public static void ClearAllSharedState()
    {
        _sharedResponses.Clear();
        _sharedStreamingResponses.Clear();
    }

    private InvokePromptResponse CreateDefaultResponse()
    {
        return new InvokePromptResponse
        {
            ChatReponseList = new List<ChatMessage>
            {
                new() { ChatRole = ChatRole.Assistant, Content = "Mock AI response" }
            },
            TokenUsageStatistics = new TokenUsageStatistics
            {
                InputToken = 10,
                OutputToken = 15,
                TotalUsageToken = 25,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };
    }

    private static async IAsyncEnumerable<object> CreateStreamingResponse(string[] responses)
    {
        foreach (var response in responses)
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, response);
            await Task.Delay(10); // Simulate streaming delay
        }
    }

    public Task<List<TextToImageResponse>?> GenerateTextToImageAsync(string prompt, TextToImageOption option, CancellationToken cancellationToken = default)
    {
        // Return a mock text-to-image response respecting the requested ResponseType
        var response = new List<TextToImageResponse>
        {
            new TextToImageResponse
            {
                ResponseType = option.ResponseType,
                Url = option.ResponseType == TextToImageResponseType.Url ? "https://mock-image.com/generated-image.jpg" : null,
                Base64Content = option.ResponseType == TextToImageResponseType.Base64Content ? "mock-base64-data" : null,
                ImageType = "png"
            }
        };
        
        return Task.FromResult<List<TextToImageResponse>?>(response);
    }
}