using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Core.Tests.TestEvents;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

var hubUrl = "http://localhost:5000/aevatarHub";
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

    var grainType = "Aevatar.Core.Tests.TestEvents";
    var grainKey = "console-test-key".ToGuid().ToString("N");
    var eventData = JsonConvert.SerializeObject(new
    {
        Greeting = "Test message"
    });

    await SendEventWithRetry(connection, grainType, grainKey, eventData);

    Console.WriteLine("✅ Success");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Abnormal: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}

Console.ReadLine();

async Task SendEventWithRetry(HubConnection conn, string type, string key, string data)
{
    const int maxRetries = 3;
    int retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            if (conn.State != HubConnectionState.Connected)
            {
                Console.WriteLine("Connection broke, retrying...");
                await conn.StartAsync();
            }

            await conn.InvokeAsync("PublishEventAsync", type, key, typeof(NaiveTestEvent).FullName!, data);
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