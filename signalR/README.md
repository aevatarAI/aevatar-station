# Aevatar SignalR

[![Orleans 9.0+](https://img.shields.io/badge/Orleans-9.0%2B-blue.svg?style=flat-square)](https://github.com/dotnet/orleans)

**SignalR Integration Layer for [Aevatar.AI](https://aevatar.ai/)**.

---

## 🌟 Overview

Aevatar SignalR is a bridging layer between the [Microsoft Orleans](https://github.com/dotnet/orleans)-based distributed actor framework (referred to as **GAgent**) and SignalR clients. Core features include:

- 🚀 **Bidirectional Communication** - Connects SignalR clients to GAgent systems
- 🎯 **Event Routing** - Delivers structured events from clients to target GAgents
- 🔄 **Response Feedback** - Asynchronously returns GAgent processing results to clients
- 🌐 **Elastic Scaling** - Manages SignalR connections using Orleans clustering capabilities

---

## Key Features

### Core Components

| Component               | Description                                                                 |
|-------------------------|-----------------------------------------------------------------------------|
| `AevatarSignalRHub`     | SignalR Hub endpoint handling client connection lifecycle                   |
| `SignalRGAgent`         | Proxy GAgent responsible for forwarding client events to target GAgents     |

### Message Protocols

**Client → GAgent Method Signature**:
```csharp
Task PublishEventAsync(
    GrainId grainId,        // Target GAgent identifier (new SignalRGAgent will interact with this GAgent)
    string eventTypeName,   // Event type name inheriting from EventBase
    string eventJson        // JSON-serialized event data
)
```
**GAgent → Client Method Name:** 
`ReceiveResponse`

# 🚀 Quick Start
## Step 1. Install Package
```bash
dotnet add package Aevatar.SignalR
```

## Step 2. Orleans Silo Configuration
```csharp
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalRSample.Host;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(silo =>
{
    silo.AddMemoryStreams(AevatarCoreConstants.StreamProvider)
        .AddMemoryGrainStorage("PubSubStore")
        .AddLogStorageBasedLogConsistencyProvider("LogStorage")
        .UseLocalhostClustering()
        .UseAevatar()
        .UseSignalR()
        .RegisterHub<AevatarSignalRHub>();
});

builder.WebHost.UseKestrel((_, kestrelOptions) =>
{
    kestrelOptions.ListenLocalhost(5001);
});

builder.Services.AddSignalR().AddOrleans();

builder.Services.AddHostedService<HostedService>();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.MapHub<AevatarSignalRHub>("/aevatarHub");
await app.RunAsync();
```

### Step 3. Client Connection Setup
```csharp
using Aevatar.Core.Abstractions.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using SignalRSample.GAgents;

const string hubUrl = "http://localhost:5001/aevatarHub";
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect() 
    .Build();

connection.On<string>("ReceiveResponse", (message) =>
{
    Console.WriteLine($"[Event] {message}");
});

try
{
    await connection.StartAsync();
    Console.WriteLine($"Init status: {connection.State}");
    var eventJson = JsonConvert.SerializeObject(new
    {
        Greeting = "Test message"
    });

    await SendEventWithRetry(connection,
        "SignalRSample.GAgents.signalR",
        "test".ToGuid().ToString("N"),
        typeof(NaiveTestEvent).FullName!,
        eventJson);

    Console.WriteLine("✅ Success");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Abnormal: {ex.Message}");
}

Console.ReadLine();

async Task SendEventWithRetry(HubConnection conn, string grainType, string grainKey, string eventTypeName, string eventJson)
{
    var grainId = GrainId.Create(grainType, grainKey);
    const int maxRetries = 3;
    var retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            if (conn.State != HubConnectionState.Connected)
            {
                Console.WriteLine("Connection broke, retrying...");
                await conn.StartAsync();
            }

            await connection.InvokeAsync("PublishEventAsync", grainId, eventTypeName, eventJson);
            return;
        }
        catch (Exception ex)
        {
            retryCount++;
            Console.WriteLine($"❌ Failed（Retry {retryCount}/{maxRetries}）: {ex.Message}");
            if (retryCount >= maxRetries)
            {
                throw;
            }
            await Task.Delay(1000 * retryCount);
        }
    }
}
```

# 🔧 Event System Implementation
## Define Event Types
```csharp
[GenerateSerializer]
public class UserLoginEvent : EventBase
{
    [Id(0)] public Guid UserId { get; set; }
    [Id(1)] public DateTimeOffset LoginTime { get; set; }
}

[GenerateSerializer]
public class LoginResponse : ResponseToPublisherEventBase
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public Guid SessionId { get; set; }
}
```
## Response Handler (GAgent Side)
```csharp
[GenerateSerializer]
public class MyGAgentState : StateBase
{
    // Define properties.
}

[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent>
{
    // Define properties.
}

[GAgent]
public class MyGAgent : GAgentBase<MyGAgentState, MyStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for demo.");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(UserLoginEvent eventData)
    {
        // Some logic.

        await PublishAsync(new LoginResponse
        {
            Success = true,
            SessionId = Guid.NewGuid()
        });
    }
}
```

# 🙏 Acknowledgements
This project utilizes code from these outstanding open-source projects:
- [SignalR.Orleans](https://github.com/OrleansContrib/SignalR.Orleans)

# License
MIT License © 2024 Aevatar Team