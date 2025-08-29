using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aevatar.GAgents.Telegram.Features.Dtos;
using Aevatar.GAgents.Telegram.Helper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.Telegram.Provider;

public class TelegramProvider : ITelegramProvider, ISingletonDependency
{
    private readonly ILogger<TelegramProvider> _logger;

    // private readonly AESCipher _aesCipher;
    public TelegramProvider(ILogger<TelegramProvider> logger)
    {
        _logger = logger;
        // _aesCipher = new AESCipher(password);
    }

    public async Task SendMessageAsync(string sendUser, string chatId, string message, ReplyParamDto? replyParam = null)
    {
        String token = GetAccount(sendUser);
        if (token.IsNullOrEmpty())
        {
            return;
        }

        string url = $"https://api.telegram.org/bot{token}/sendMessage";

        // Create a request object
        var sendMessageRequest = new MessageParamsRequest()
        {
            ChatId = chatId,
            Text = message
        };

        // Add reply_to_message_id if present
        if (replyParam != null)
        {
            var replyParameters = new ReplyParameters
            {
                MessageId = replyParam.MessageId.ToString()
            };
            sendMessageRequest.ReplyParameters = replyParameters;
        }

        // Serialize the request object to JSON
        string json = JsonConvert.SerializeObject(sendMessageRequest, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        try
        {
            _logger.LogDebug("send message to {chatId} : {message}", chatId, message);
            var response =
                await new HttpClient().PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request error: {e.Message}");
        }
    }

    public async Task<string> GetUpdatesAsync(string sendUser)
    {
        String token = GetAccount(sendUser);
        if (token.IsNullOrEmpty())
        {
            return "";
        }

        string url = $"https://api.telegram.org/bot{token}/GetUpdates";
        try
        {
            // var telUpdate = await GetAsync<TelegramUpdateDto>(url);
            HttpResponseMessage response = await new HttpClient().GetAsync(url);

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
            var res = await response.Content.DeserializeSnakeCaseAsync<GetUpdatedDto>();
            _logger.LogInformation("GetUpdatesAsync:{message}", JsonConvert.SerializeObject(res));
            return responseBody;
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request error: {e.Message}");
        }

        return null;
    }


    public async Task SetWebhookAsync(string sendUser, string webhook, string token)
    {
        String account = GetAccount(token);
        if (account.IsNullOrEmpty())
        {
            return;
        }

        string url = $"https://api.telegram.org/bot{account}/setWebhook";

        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("url", webhook),
            new KeyValuePair<string, string>("secret_token", sendUser)
        });

        try
        {
            HttpResponseMessage response = await new HttpClient().PostAsync(url, parameters);

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request error: {e.Message}");
        }
    }

    public async Task DelWebhookAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token is null or empty");
            return;
        }

        String account = GetAccount(token);
        if (string.IsNullOrWhiteSpace(account))
        {
            _logger.LogWarning("Account derived from token is null or empty");
            return;
        }

        string url = $"https://api.telegram.org/bot{account}/deleteWebhook";
        try
        {
            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(responseBody);
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"Request error: {e.Message}");
        }
    }

    private string GetAccount(string accountName)
    {
        // try
        // {
        //   return  _aesCipher.Decrypt(accountName);
        // }
        // catch (Exception e)
        // {
        //     _logger.LogError(e,$"Decrypt error: {accountName}");
        // }
        return accountName;
    }

    public async Task SendPhotoAsync(string sendUser, PhotoParamsRequest photoParamsRequest)
    {
        var token = GetAccount(sendUser);
        if (token.IsNullOrEmpty())
        {
            return;
        }

        var url = $"https://api.telegram.org/bot{token}/sendPhoto";
        var paramsJson = JsonConvert.SerializeObject(photoParamsRequest, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        var content = new StringContent(paramsJson, Encoding.UTF8, "application/json");
        try
        {
            var response = await new HttpClient().PostAsync(url, content);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request error: {e.Message}");
        }
    }
}