// ABOUTME: This file provides ABP module for registering mock LLM services in tests
// ABOUTME: Replaces real AI service dependencies with mock implementations for unit testing

using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.AIGAgent.Test.Modules;

public class MockBrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Note: The following services are already registered in ClusterFixture (Orleans Silo):
        // - IBrainFactory (MockBrainFactory)
        // - IOptions<SystemLLMConfigOptions>
        // - IBytePlusModelArkClient (MockBytePlusModelArkClient)
        // - HttpClient / IHttpClientFactory (TestHttpClientFactory)
        // DO NOT re-register them here to avoid service resolution conflicts.
        //
        // Orleans Grains (including VideoGenerationGAgent) run in the Silo's DI container,
        // not in the ABP DI container. Services registered here would not be accessible to Grains.
        
        // Only register test-specific mock services that are NOT in ClusterFixture
        services.AddTransient<MockChatBrain>();
        services.AddTransient<MockTextToImageBrain>();
    }
}