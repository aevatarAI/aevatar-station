using System;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Orleans.Runtime;
using SignalRSample.GAgents;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers
{
    [Route("api/health")]
    public class HealthController : AbpController
    {
        private string _hubUrl = "http://localhost:8001/api/agent/aevatarHub"; 
        private HubConnection _connection;  
        private readonly object _lock = new object();  

        
        public HealthController()
        {
            InitializeConnection();
        }

        [HttpGet("signalR")]
        public async Task<string> CheckSignalRHealth([FromQuery] string? url = null)
        {
            try
            {
                _hubUrl = url ?? _hubUrl;
                if (_connection?.State != HubConnectionState.Connected)
                {
                    await _connection.StartAsync();
                }

                // 发布测试事件
                await PublishEventAsync("PublishEventAsync");
                await PublishEventAsync("SubscribeAsync");

                
                return "200";
            }
            catch (Exception ex)
            {
                return "500:"+ex.Message;
            }
        }

        private void InitializeConnection()
        {
            lock (_lock)
            {
                if (_connection == null)
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(_hubUrl)
                        .WithAutomaticReconnect()  
                        .Build();
 
                    _connection.On<string>("ReceiveResponse", (message) =>
                    {
                        Console.WriteLine($"[Event] {message}");
                    });
                }
            }
        }
        
        private async Task PublishEventAsync(string methodName)
        {
            try
            {
                var eventJson = JsonConvert.SerializeObject(new
                {
                    Greeting = "Test message"
                });

                
                await SendEventWithRetry(_connection, methodName,
                    "SignalRSample.GAgents.Aevatar.SignalRDemo",
                    "test".ToGuid().ToString("N"),
                    typeof(NaiveTestEvent).FullName!,
                    eventJson);

                Console.WriteLine("✅ Event Published Successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Abnormal Error: {ex.Message}");
                throw; 
            }
        }

       
        private async Task SendEventWithRetry(HubConnection conn, string methodName, string grainType, string grainKey, string eventTypeName, string eventJson)
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
                        Console.WriteLine("Connection is broken, retrying...");
                        await conn.StartAsync();
                    }

                     
                    var signalRGAgentGrainId = await conn.InvokeAsync<GrainId>(methodName, grainId, eventTypeName, eventJson);

                    // 成功响应
                    Console.WriteLine($"SignalRGAgentGrainId: {signalRGAgentGrainId}");
                    return;  
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"❌ Retry {retryCount}/{maxRetries} Failed: {ex.Message}");

                    if (retryCount >= maxRetries)
                    {
                        throw;
                    }

                    await Task.Delay(1000 * retryCount);
                }
            }
        }
    }
}