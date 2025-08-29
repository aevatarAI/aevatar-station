// See https://aka.ms/new-console-template for more information

using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.Grain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams("InMemoryStreamProvider");
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();
builder.ConfigureServices((context, service) =>
{
    service.AddSingleton<IGAgentFactory, GAgentFactory>();
});

using IHost host = builder.Build();
await host.StartAsync();

IGAgentFactory agentFactory = host.Services.GetRequiredService<IGAgentFactory>();

IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
var groupAgent = client.GetGrain<IGroupGAgent>(Guid.NewGuid());

var jack = client.GetGrain<IWorker>(Guid.NewGuid());
await jack.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jack" });
var fred = client.GetGrain<IWorker>(Guid.NewGuid());
await fred.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fred" });
var ace = client.GetGrain<IWorker>(Guid.NewGuid());
await ace.ConfigAsync(new GroupMemberConfigDto() { MemberName = "ace" });
var sony = client.GetGrain<IWorker>(Guid.NewGuid());
await sony.ConfigAsync(new GroupMemberConfigDto() { MemberName = "sony" });
var doni = client.GetGrain<IWorker>(Guid.NewGuid());
await doni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "doni" });

var leader = client.GetGrain<ILeader>(Guid.NewGuid());
await leader.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Leader" });

var workerflow = new List<WorkflowUnitDto>()
{
    new WorkflowUnitDto()
    {
        GrainId = jack.GetGrainId().ToString(),
        NextGrainId = fred.GetGrainId().ToString(),
    },
    new WorkflowUnitDto()
    {
        GrainId = ace.GetGrainId().ToString(),
        NextGrainId = fred.GetGrainId().ToString(),
    },
    new WorkflowUnitDto()
    {
        GrainId = fred.GetGrainId().ToString(),
        NextGrainId = doni.GetGrainId().ToString(),
    },
    new WorkflowUnitDto()
    {
        GrainId = sony.GetGrainId().ToString(),
        NextGrainId = doni.GetGrainId().ToString(),
    },
    new WorkflowUnitDto()
    {
        GrainId = doni.GetGrainId().ToString(),
        NextGrainId = leader.GetGrainId().ToString(),
    },
    new WorkflowUnitDto()
    {
        GrainId = leader.GetGrainId().ToString(),
        NextGrainId = "",
    }
};

await groupAgent.AddWorkflowGroupChat(agentFactory, workerflow);
await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

// await groupAgent.RegisterAsync(jack);
// await groupAgent.RegisterAsync(fred);
// await groupAgent.RegisterAsync(leader);
// await groupAgent.AddGroupChat(client, "Will Artificial Intelligence Replace Human Creativity ?");

await Task.Delay(TimeSpan.FromSeconds(1000));