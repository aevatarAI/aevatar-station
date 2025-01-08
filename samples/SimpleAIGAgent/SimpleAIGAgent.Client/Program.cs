using Aevatar.AI.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimpleAIGAgent.Grains.Agents.Chat;
using SimpleAIGAgent.Grains.Agents.Events;
using SimpleAIGAgent.Grains.Agents.Publisher;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams("InMemoryStreamProvider");
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

IClusterClient client = host.Services.GetRequiredService<IClusterClient>();

// IHello friend = client.GetGrain<IHello>(0);
// string response = await friend.SayHello("Hi friend!");

var chatAgentId = Guid.NewGuid();
var chatAgent = client.GetGrain<IChatAIGAgent>(chatAgentId);
await chatAgent.InitializeAsync(new InitializeDto()
{
    Files = [],
    Instructions = "{{prompt}}",
    LLM = "AzureOpenAI"
});

var publisher = client.GetGrain<IPublishingGAgent>(Guid.NewGuid());
await chatAgent.RegisterAsync(publisher);
await publisher.PublishEventAsync(new ChatEvent()
{
    Message = "Tell me about Einstein's theory of relativity."
});

Console.WriteLine($"""
                   Press any key to exit...
                   """);

Console.ReadKey();

await host.StopAsync();