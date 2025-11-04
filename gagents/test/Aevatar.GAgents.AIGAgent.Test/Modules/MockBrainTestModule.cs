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
using System.Net.Http;
using Moq;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System;

namespace Aevatar.GAgents.AIGAgent.Test.Modules;

public class MockBrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Register Mock HttpClient for VideoGenerationGAgent with BytePlus API mocking
        services.AddSingleton<HttpClient>(provider =>
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            
            // Mock BytePlus video generation task creation (POST to /api/v3/contents/generations/tasks)
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri != null && 
                        req.RequestUri.ToString().EndsWith("/api/v3/contents/generations/tasks")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var taskId = Guid.NewGuid().ToString();
                    var responseContent = JsonConvert.SerializeObject(new { Id = taskId, Status = "submitted" });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                    };
                });

            // Mock BytePlus video generation task status check (GET to /api/v3/contents/generations/tasks/{taskId})
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri != null && 
                        req.RequestUri.ToString().Contains("/api/v3/contents/generations/tasks/") &&
                        req.RequestUri.Segments.Length > 5), // Has task ID segment
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var responseContent = JsonConvert.SerializeObject(new 
                    { 
                        Status = "succeeded", 
                        VideoUrl = "https://demo-video-storage.example.com/videos/test-video.mp4"
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                    };
                });

            // Default fallback for any other HTTP requests
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"success\": true}", Encoding.UTF8, "application/json")
                });

            return new HttpClient(mockHttpHandler.Object);
        });

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
                },
                ["BytePlusVideoGeneration"] = new LLMConfig
                {
                    ProviderEnum = LLMProviderEnum.BytePlus,
                    ModelIdEnum = ModelIdEnum.BytePlusVideoGeneration,
                    ModelName = "test-video-model",
                    Endpoint = "https://ark.ap-southeast.bytepluses.com",
                    ApiKey = "test-byteplus-key"
                }
            }
        };
        services.AddSingleton<IOptions<SystemLLMConfigOptions>>(new OptionsWrapper<SystemLLMConfigOptions>(systemLLMConfigOptions));
    }
}