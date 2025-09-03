using System;
using System.Collections.Generic;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Extensions;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.McpClient;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Test.Mocks;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Aevatar.Plugins;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using Moq;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Orleans.SyncWork;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aws;
using Volo.Abp.Caching;
using Volo.Abp.Caching.Hybrid;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.MultiTenancy.ConfigurationStore;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;
using Volo.Abp.Settings;

namespace Aevatar.GAgents.TestBase;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public static MockLoggerProvider LoggerProvider { get; set; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }

    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.secrets.json", true)
                .Build();
            

            hostBuilder.ConfigureServices(services =>
                {
                    //services.AddAutoMapper(typeof(AIApplicationGrainsModule).Assembly);
                    var mock = new Mock<ILocalEventBus>();
                    services.AddSingleton(typeof(ILocalEventBus), mock.Object);
                    
                    // Configure logging
                    var loggerProvider = new MockLoggerProvider("Aevatar");
                    services.AddSingleton<ILoggerProvider>(loggerProvider);
                    LoggerProvider = loggerProvider;
                    services.AddLogging(logging =>
                    {
                        logging.AddConsole(); // Adds console logger
                    });
                    services.OnExposing(onServiceExposingContext =>
                    {
                        var implementedTypes = ReflectionHelper.GetImplementedGenericTypes(
                            onServiceExposingContext.ImplementationType,
                            typeof(IObjectMapper<,>)
                        );
                    });

                    services.AddTransient(typeof(IObjectMapper<>), typeof(DefaultObjectMapper<>));
                    services.AddTransient(typeof(IObjectMapper), typeof(DefaultObjectMapper));
                    services.AddTransient(typeof(IAutoObjectMappingProvider),
                        typeof(AutoMapperAutoObjectMappingProvider));
                    services.AddTransient<IMapperAccessor>(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    
                    // var configuration = services.GetConfiguration();
                    services.Configure<QdrantConfig>(configuration.GetSection("VectorStores:Qdrant"));
                    services.Configure<AzureOpenAIEmbeddingsConfig>(configuration.GetSection("AIServices:AzureOpenAIEmbeddings"));
                    services.Configure<RagConfig>(configuration.GetSection("Rag"));
                    
                    // Register SystemLLMConfigOptions for Orleans grains
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
                    services.AddSingleton<IBlobContainer, MockBlobContainer>();
                    
                    services.AddSemanticKernel()
                        .AddQdrantVectorStore()
                        .AddAzureOpenAITextEmbedding();
                    
                    services.AddSingleton<IKernelBuilderFactory, MockKernelBuilderFactory>();
                    services.AddSingleton<IBrainFactory, MockBrainFactory>();
                    // Add IGAgentFactory registration for Orleans grain dependency injection
                    services.AddSingleton<IGAgentFactory, GAgentFactory>();
                    services.AddSingleton<IGAgentService, GAgentService>();
                    services.AddSingleton<IGAgentExecutor, GAgentExecutor>();
                    services.AddSingleton<IGAgentManager, GAgentManager>();
                    services.AddSingleton<IPluginGAgentManager, PluginGAgentManager>();
                    // 注册Mock MCP客户端提供者用于测试（与TestBase保持一致使用Singleton）
                    services.AddSingleton<IMcpClientProvider, MockMcpClientProvider>();
                    services.AddSingleton<MockMcpClientProvider>();
                    
                    // Register HttpClientFactory used by grains during tests
                    services.AddHttpClient();
                    services.AddSingleton<IHttpClientFactory, Aevatar.GAgents.TestBase.Http.TestHttpClientFactory>();
                })
                .UseAevatar(true)
                .AddMemoryStreams("Aevatar")
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault()
                .AddLogStorageBasedLogConsistencyProvider("LogStorage");
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }

    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddMemoryStreams("Aevatar");
    }
}