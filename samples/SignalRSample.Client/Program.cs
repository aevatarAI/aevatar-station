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

await connection.StartAsync();
Console.WriteLine($"Init status: {connection.State}");

Console.WriteLine("Select an option:");
Console.WriteLine("1. Publish Event to aevatar GAgent - Fire and Forget");
Console.WriteLine("2. Subscribe aevatar GAgent");
Console.WriteLine("3. Unsubscribe aevatar GAgent");
var choice = Console.ReadLine();

while (true)
{
    switch (choice)
    {
        case "1":
            await PublishEventAsync("PublishEventAsync");
            break;
        case "2":
            await PublishEventAsync("SubscribeAsync");
            break;
        case "3":
            Console.WriteLine("Enter grainId:");
            var grainIdString = Console.ReadLine();
            var grainId = GrainId.Parse(grainIdString!);
            await connection.InvokeAsync("UnsubscribeAsync", grainId);
            break;
        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
    
    choice = Console.ReadLine();
}

async Task PublishEventAsync(string methodName)
{
    try
    {
        var eventJson = JsonConvert.SerializeObject(new
        {
            Greeting = "Test message"
        });

        await SendEventWithRetry(connection, methodName,
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
}

async Task SendEventWithRetry(HubConnection conn, string methodName, string grainType, string grainKey, string eventTypeName, string eventJson)
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

            await connection.InvokeAsync(methodName, grainId, eventTypeName, eventJson);
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