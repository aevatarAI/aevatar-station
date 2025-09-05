// ABOUTME: This file provides ABP module for registering mock LLM services in tests
// ABOUTME: Replaces real AI service dependencies with mock implementations for unit testing

using System.Collections.Generic;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.AIGAgent.Test.Modules;

public class MockBrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Replace real BrainFactory with mock implementation
        services.AddSingleton<IBrainFactory, MockBrainFactory>();
        
        // Optional: Register individual mock brains if needed for direct injection
        services.AddTransient<MockChatBrain>();
        services.AddTransient<MockTextToImageBrain>();
        
        // Register SystemLLMConfigOptions for tests
        var systemLLMConfigOptions = new SystemLLMConfigOptions
        {
            SystemLLMConfigs = new Dictionary<string, LLMConfig>
            {
                ["OpenAI"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.Azure,
                    ModelIdEnum = ModelIdEnum.OpenAI,
                    ModelName = "gpt-4o",
                    Endpoint = "https://test.openai.azure.com",
                    ApiKey = "test-key"
                },
                ["DeepSeek"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.Azure,
                    ModelIdEnum = ModelIdEnum.DeepSeek,
                    ModelName = "DeepSeek-R1",
                    Endpoint = "https://test.deepseek.azure.com",
                    ApiKey = "test-key"
                },
                ["OpenAITextToImage"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.Azure,
                    ModelIdEnum = ModelIdEnum.OpenAITextToImage,
                    ModelName = "dall-e-3",
                    Endpoint = "https://test.openai.azure.com",
                    ApiKey = "test-key"
                },
                ["Azure"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.Azure,
                    ModelIdEnum = ModelIdEnum.OpenAI,
                    ModelName = "gpt-4o",
                    Endpoint = "https://test.azure.openai.com",
                    ApiKey = "test-key"
                },
                ["Google"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.Google,
                    ModelIdEnum = ModelIdEnum.Gemini,
                    ModelName = "gemini-pro",
                    Endpoint = "https://test.google.ai",
                    ApiKey = "test-key"
                }
            }
        };
        services.AddSingleton<IOptions<SystemLLMConfigOptions>>(new OptionsWrapper<SystemLLMConfigOptions>(systemLLMConfigOptions));
    }
}