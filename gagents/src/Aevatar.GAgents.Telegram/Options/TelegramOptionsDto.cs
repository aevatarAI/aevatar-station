using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.Telegram.Options;

[GenerateSerializer]
public class TelegramOptionsDto : ConfigurationBase
{
    [Id(0)]
    public string Webhook { get; set; } = "https://your-domain.com/webhook";
    
    [Id(1)]
    public string EncryptionPassword { get; set; } = "YOUR_ENCRYPTION_PASSWORD";
    
    [Id(2)]
    public string BotToken { get; set; } = "YOUR_BOT_TOKEN";
    
    [Id(3)]
    public int MaxConnections { get; set; } = 100;
}