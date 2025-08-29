using System;
using System.Threading.Tasks;
using Aevatar.GAgents.Telegram.Features.Dtos;
using Aevatar.GAgents.Telegram.Provider;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.Telegram.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class TelegramGrain : Grain, ITelegramGrain
{
    private readonly ITelegramProvider _telegramProvider;

    private ILogger<TelegramGrain> _logger;

    // private readonly IOptionsMonitor<TelegramOptionsDto> _telegramOptions;
    public TelegramGrain(ITelegramProvider telegramProvider, ILogger<TelegramGrain> logger)
    {
        _telegramProvider = telegramProvider;
        _logger = logger;
        // _telegramOptions = telegramOptions;
    }


    public async Task SendMessageAsync(string sendUser, string chatId, string message, string? replyMessageId)
    {
        ReplyParamDto replyParamDto = null;
        if (!replyMessageId.IsNullOrEmpty())
        {
            replyParamDto = new ReplyParamDto()
            {
                MessageId = long.Parse(replyMessageId)
            };
        }

        await _telegramProvider.SendMessageAsync(sendUser, chatId, message, replyParamDto);
    }

    public async Task RegisterTelegramAsync(string webhook, string sendUser, string token)
    {
        await _telegramProvider.SetWebhookAsync(sendUser, webhook, token);
    }

    public async Task UnRegisterTelegramAsync(string token)
    {
        await _telegramProvider.DelWebhookAsync(token);
    }
}