using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

var hubUrl = "http://localhost:5000/aevatarHub";
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect() 
    .Build();

// ----------------- 监听事件（可选） -----------------
connection.On<string>("ReceiveResponse", (message) =>
{
    Console.WriteLine($"[Event] {message}");
});

try
{
    // 首次启动连接
    await connection.StartAsync();
    Console.WriteLine($"初始状态: {connection.State}");

    // --------------- 发送测试事件（带状态检查）---------------
    var grainType = "TestGrain";
    var grainKey = "console-test-key";
    var eventData = JsonConvert.SerializeObject(new
    {
        Message = "Test message"
    });

    // 安全的发送逻辑
    await SendEventWithRetry(connection, grainType, grainKey, eventData);

    Console.WriteLine("✅ 事件发送成功");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 全局异常: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}

Console.ReadLine();

// ----------------- 带重试的事件发送方法 -----------------
async Task SendEventWithRetry(HubConnection conn, string type, string key, string data)
{
    const int maxRetries = 3;
    int retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            // 检查连接状态
            if (conn.State != HubConnectionState.Connected)
            {
                Console.WriteLine("连接已断开，正在重连...");
                await conn.StartAsync();
            }

            await conn.InvokeAsync("PublishEventAsync", type, key, data);
            return; // 发送成功则退出
        }
        catch (Exception ex)
        {
            retryCount++;
            Console.WriteLine($"❌ 发送失败（重试 {retryCount}/{maxRetries}）: {ex.Message}");
            if (retryCount >= maxRetries)
            {
                throw; // 抛出异常由外层处理
            }
            await Task.Delay(1000 * retryCount); // 延迟重试
        }
    }
}