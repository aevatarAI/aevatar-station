using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<QdrantConfig>(context.Configuration.GetSection("VectorStores:Qdrant"));
        services.Configure<SystemLLMConfigOptions>(context.Configuration);
        services.Configure<AzureOpenAIEmbeddingsConfig>(
            context.Configuration.GetSection("AIServices:AzureOpenAIEmbeddings"));
        services.Configure<RagConfig>(context.Configuration.GetSection("Rag"));

        services.AddSemanticKernel()
            .AddQdrantVectorStore()
            .AddAzureOpenAITextEmbedding();
    })
    .UseConsoleLifetime();

using var host = builder.Build();

await host.RunAsync();