using Aevatar.AI.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams("InMemoryStreamProvider")
            .AddMemoryGrainStorage("PubSubStore")
            .UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSemanticKernel()
            .AddAzureOpenAI()
            .AddQdrantVectorStore()
            .AddAzureOpenAITextEmbedding();
    })
    .UseConsoleLifetime();

using var host = builder.Build();

await host.RunAsync();