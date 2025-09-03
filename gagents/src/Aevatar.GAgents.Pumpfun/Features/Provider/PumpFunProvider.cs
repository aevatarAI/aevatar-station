using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aevatar.GAgents.PumpFun.Features.Common;
using Aevatar.GAgents.PumpFun.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.PumpFun.Provider;

public class PumpFunProvider : IPumpFunProvider,ISingletonDependency
{
    private readonly ILogger<PumpFunProvider> _logger;
    private readonly IOptionsMonitor<PumpfunOptions> _pumpfunOptions;
    private readonly string _callBackUrl;
    private readonly string _accessToke;

    public PumpFunProvider(ILogger<PumpFunProvider> logger, IOptionsMonitor<PumpfunOptions> pumpfunOptions)
    {
        _logger = logger;
        _callBackUrl = pumpfunOptions.CurrentValue.CallBackUrl;
        _accessToke = pumpfunOptions.CurrentValue.AccessToken;
    }
    
    public async Task SendMessageAsync(string replyId, string replyMessage)
    {
        var sendMessageRequest = new PumpfunResponse()
        {
            ReplyId = replyId,
            ReplyMessage = replyMessage
        };
        // Serialize the request object to JSON
        var json = JsonConvert.SerializeObject(sendMessageRequest, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        try
        {
            _logger.LogDebug("PumpFunProvider send message to {replyId} : {replyMessage}",replyId, replyMessage);
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_callBackUrl);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _accessToke);
            _logger.LogDebug("PumpFunProvider send message2, accessToken:" + _accessToke);
            var response = await client.PostAsync(_callBackUrl, new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("PumpFunProvider send message3 to {replyId} : {response} : {replyMessage}",replyId, response, replyMessage);
            string responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request error: {e.Message}");
        }
    }
}