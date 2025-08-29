// ABOUTME: This file implements a mock IBrainFactory for unit testing
// ABOUTME: Creates mock brain instances without real AI service dependencies

using System.Collections.Concurrent;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockBrainFactory : IBrainFactory
{
    // Cache brain instances by configuration to ensure consistency
    private static readonly ConcurrentDictionary<string, IChatBrain> _chatBrainCache = new();
    private static readonly ConcurrentDictionary<string, ITextToImageBrain> _textToImageBrainCache = new();

    public IBrain? CreateBrain(LLMProviderConfig llmProviderConfig)
    {
        // Initialize the llmConfig for brain initialization
        var llmConfig = new LLMConfig
        {
            ProviderEnum = llmProviderConfig.ProviderEnum,
            ModelIdEnum = llmProviderConfig.ModelIdEnum
        };
        
        IBrain mockBrain;
        
        // Create appropriate brain type based on ModelIdEnum
        if (llmProviderConfig.ModelIdEnum == ModelIdEnum.OpenAITextToImage)
        {
            // Create a MockTextToImageBrain for text-to-image models
            mockBrain = new MockTextToImageBrain();
            Console.WriteLine($"[MockBrainFactory] Created MockTextToImageBrain for ModelId: {llmProviderConfig.ModelIdEnum}");
        }
        else
        {
            // Create a MockChatBrain for chat models (implements both interfaces)
            mockBrain = new MockChatBrain();
            Console.WriteLine($"[MockBrainFactory] Created MockChatBrain for ModelId: {llmProviderConfig.ModelIdEnum}");
        }
        
        // Synchronously set the configuration (mock doesn't need async initialization)
        mockBrain.InitializeAsync(llmConfig, "mock-brain-id", "Mock brain for testing").Wait();
        
        // Verify that the brain implements the expected interfaces
        Console.WriteLine($"[MockBrainFactory] Created brain type: {mockBrain?.GetType()?.FullName}");
        Console.WriteLine($"[MockBrainFactory] Implements IChatBrain: {mockBrain is IChatBrain}");
        Console.WriteLine($"[MockBrainFactory] Implements ITextToImageBrain: {mockBrain is ITextToImageBrain}");
        
        return mockBrain;
    }

    // Overload to support LLMConfig (converts to LLMProviderConfig)
    public IBrain? CreateBrain(LLMConfig llmConfig)
    {
        var providerConfig = new LLMProviderConfig
        {
            ProviderEnum = llmConfig.ProviderEnum,
            ModelIdEnum = llmConfig.ModelIdEnum
        };
        return CreateBrain(providerConfig);
    }

    public IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig)
    {
        var key = $"{llmProviderConfig.ProviderEnum}_{llmProviderConfig.ModelIdEnum}";
        
        return _chatBrainCache.GetOrAdd(key, _ =>
        {
            var mockBrain = new MockChatBrain();
            
            // Initialize the mock brain with the provider configuration
            var llmConfig = new LLMConfig
            {
                ProviderEnum = llmProviderConfig.ProviderEnum,
                ModelIdEnum = llmProviderConfig.ModelIdEnum
            };
            
            // Synchronously set the configuration (mock doesn't need async initialization)
            mockBrain.InitializeAsync(llmConfig, "mock-brain-id", "Mock brain for testing").Wait();
            
            Console.WriteLine($"[GetChatBrain] Created brain implements IChatBrain: {mockBrain is IChatBrain}");
            Console.WriteLine($"[GetChatBrain] Created brain implements ITextToImageBrain: {mockBrain is ITextToImageBrain}");
            
            return mockBrain;
        });
    }

    public ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig)
    {
        var key = $"{llmProviderConfig.ProviderEnum}_{llmProviderConfig.ModelIdEnum}";
        
        return _textToImageBrainCache.GetOrAdd(key, _ =>
        {
            var mockBrain = new MockTextToImageBrain();
            
            // Initialize the mock brain with the provider configuration
            var llmConfig = new LLMConfig
            {
                ProviderEnum = llmProviderConfig.ProviderEnum,
                ModelIdEnum = llmProviderConfig.ModelIdEnum
            };
            
            // Synchronously set the configuration (mock doesn't need async initialization)
            mockBrain.InitializeAsync(llmConfig, "mock-text-to-image-brain-id", "Mock text-to-image brain for testing").Wait();
            
            return mockBrain;
        });
    }
    
    // Public method to clear all cached brains (for test cleanup)
    public static void ClearAllCaches()
    {
        _chatBrainCache.Clear();
        _textToImageBrainCache.Clear();
        MockChatBrain.ClearAllSharedState();
        MockTextToImageBrain.ClearAllSharedState();
    }
}