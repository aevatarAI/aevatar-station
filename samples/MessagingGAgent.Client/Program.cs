using Aevatar.Core.Abstractions;
using MessagingGAgent.Grains.Agents.Events;
using MessagingGAgent.Grains.Agents.Messaging;
using MessagingGAgent.Grains.Agents.Publisher;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

var client = host.Services.GetRequiredService<IClusterClient>();

var parentId = Guid.NewGuid();
//var messagingParentAgent = client.GetGrain<IMessagingGAgent>(parentId);
var parentAgent = client.GetGrain<IGAgent>(parentId);

List<IMessagingGAgent> messagingAgents = [];
var maxAgents = 2;
for(var i = 0; i < maxAgents; ++i)
{
    var messagingAgentId = Guid.NewGuid();
    var messagingAgent = client.GetGrain<IMessagingGAgent>(messagingAgentId);
    await parentAgent.RegisterAsync(messagingAgent);
    
    messagingAgents.Add(messagingAgent);
}

var publisher = client.GetGrain<IPublishingGAgent>(Guid.NewGuid());
await parentAgent.RegisterAsync(publisher);
await publisher.PublishEventAsync(new SendEvent()
{
    Message = "Hello, World!"
});

await Task.Delay(5000);

var completed = 0;
foreach (var agent in messagingAgents)
{
    var receivedMessages = await agent.GetReceivedMessagesAsync();
    if (receivedMessages != maxAgents + 1)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("Agent did not receive the expected number of messages. " + receivedMessages);

        continue;
    }
    completed++;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.Write("Completed: " + completed);

await host.StopAsync();