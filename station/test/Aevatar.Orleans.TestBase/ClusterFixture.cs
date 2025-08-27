using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar;
using Aevatar.Application.Grains;
using Aevatar.CQRS;
using Aevatar.CQRS.Handler;
using Aevatar.CQRS.Provider;
using Aevatar.Mock;
using Aevatar.Options;
using Aevatar.Service;
using AutoMapper;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using Moq;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.AutoMapper;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

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
                // .AddJsonFile("appsettings.secrets.json")
                .Build();

            hostBuilder.ConfigureServices(services =>
                {
                    services.AddAutoMapper(typeof(AIApplicationGrainsModule).Assembly);
                    var mock = new Mock<ILocalEventBus>();
                    services.AddSingleton(typeof(ILocalEventBus), mock.Object);
                    services.AddMemoryCache();
                    // Configure logging
                    var loggerProvider = new MockLoggerProvider("Aevatar");
                    services.AddSingleton<ILoggerProvider>(loggerProvider);
                    LoggerProvider = loggerProvider;
                    services.AddLogging(logging =>
                    {
                        //logging.AddProvider(loggerProvider);
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
                    services.AddTransient(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    //services.AddMediatR(typeof(TestSiloConfigurations).Assembly);

                    services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());


                    services.AddSingleton<IIndexingService, MockElasticIndexingService>();

                    services.AddSingleton<ElasticsearchClient>(sp =>
                    {
                        var response =
                            TestableResponseFactory.CreateSuccessfulResponse<SearchResponse<Document>>(new(), 200);
                        var mock = new Mock<ElasticsearchClient>();
                        mock
                            .Setup(m => m.SearchAsync<Document>(It.IsAny<SearchRequest>(),
                                It.IsAny<CancellationToken>()))
                            .ReturnsAsync(response);
                        return mock.Object;
                    });
                    services.AddMediatR(cfg =>
                        cfg.RegisterServicesFromAssembly(typeof(SaveStateBatchCommandHandler).Assembly)
                    );
                    services.AddSingleton(typeof(ICQRSProvider), typeof(CQRSProvider));
                    services.AddSingleton(typeof(ICqrsService), typeof(CqrsService));
                    
                    // Add mock IBrainFactory for AIGAgent testing
                    try
                    {
                        var brainFactoryType = Type.GetType("Aevatar.GAgents.AI.BrainFactory.IBrainFactory, Aevatar.GAgents.AI.Abstractions");
                        if (brainFactoryType != null)
                        {
                            // Use reflection to create Mock.Of<T> with the correct type
                            var mockOfMethod = typeof(Mock).GetMethods()
                                .Where(m => m.Name == "Of" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                                .FirstOrDefault();
                            
                            if (mockOfMethod != null)
                            {
                                var genericMockOfMethod = mockOfMethod.MakeGenericMethod(brainFactoryType);
                                var mockBrainFactory = genericMockOfMethod.Invoke(null, null);
                                services.AddSingleton(brainFactoryType, mockBrainFactory);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but don't fail the test setup
                        Console.WriteLine($"Warning: Could not register IBrainFactory mock: {ex.Message}");
                    }
                })
                .AddMemoryStreams("Aevatar")
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault()
                .AddLogStorageBasedLogConsistencyProvider("LogStorage")
                .Configure<NameContestOptions>(configuration.GetSection("NameContest"));
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

    public static async Task WaitLogAsync(string log)
    {
        var timeout = TimeSpan.FromSeconds(15);
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (LoggerProvider.Logs.Any(l => l.Contains(log)))
            {
                break;
            }

            await Task.Delay(1000);
        }
    }
}