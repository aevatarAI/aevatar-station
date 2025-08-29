// ABOUTME: This file implements a mock ITextToImageBrain for unit testing
// ABOUTME: Provides configurable responses without real AI service calls

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockTextToImageBrain : ITextToImageBrain
{
    // Shared state across all instances
    private static readonly ConcurrentDictionary<string, List<TextToImageResponse>> _sharedResponses = new();
    
    private List<TextToImageResponse>? _nextResponse;
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

    public Task<List<TextToImageResponse>?> GenerateTextToImageAsync(string prompt, TextToImageOption option,
        CancellationToken cancellationToken = default)
    {
        // Check shared state first, then instance state, then default
        List<TextToImageResponse>? response = null;
        
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
            response = CreateDefaultResponse(option);
        }
        
        return Task.FromResult<List<TextToImageResponse>?>(response);
    }

    public void SetNextResponse(List<TextToImageResponse> response)
    {
        // Store in shared state so it persists across instances
        _sharedResponses.AddOrUpdate(_brainKey, response, (key, oldValue) => response);
        
        // Also set instance state for backward compatibility
        _nextResponse = response;
    }
    
    // Public method to set responses by configuration (for tests that can't get the brain instance)
    public static void SetNextResponseForConfig(LLMProviderEnum providerEnum, ModelIdEnum modelIdEnum, List<TextToImageResponse> response)
    {
        var key = $"{providerEnum}_{modelIdEnum}";
        _sharedResponses.AddOrUpdate(key, response, (k, oldValue) => response);
    }
    
    // Public method to clear all shared state (for test cleanup)
    public static void ClearAllSharedState()
    {
        _sharedResponses.Clear();
    }

    private List<TextToImageResponse> CreateDefaultResponse(TextToImageOption option)
    {
        var responses = new List<TextToImageResponse>();
        var count = option.Count > 0 ? option.Count : 1;

        for (int i = 0; i < count; i++)
        {
            var response = new TextToImageResponse
            {
                ResponseType = option.ResponseType,
                ImageType = "png"
            };

            if (option.ResponseType == TextToImageResponseType.Url)
            {
                response.Url = $"https://mock-ai-service.com/image-{i + 1}.png";
                response.Base64Content = "";
            }
            else
            {
                // Small 1x1 transparent PNG as base64
                response.Base64Content = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
                response.Url = "";
            }

            responses.Add(response);
        }

        return responses;
    }
}