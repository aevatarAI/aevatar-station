using System.Threading.Tasks;
using Orleans;

namespace Aevatar.GAgents.Telegram.Grains;

public interface ITelegramGrain : IGrainWithStringKey
{
    public Task SendMessageAsync(string sendUser, string chatId, string message,
        string? replyMessageId);

    public Task RegisterTelegramAsync(string webhook,string sendUser, string token);

    Task UnRegisterTelegramAsync(string stateBotName);
}