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

            await connection.InvokeAsync("SubscribeAsync", grainId, eventTypeName, eventJson);
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