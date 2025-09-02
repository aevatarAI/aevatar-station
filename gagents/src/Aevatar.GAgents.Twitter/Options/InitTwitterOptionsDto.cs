using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.Twitter.Options;

[GenerateSerializer]
public class InitTwitterOptionsDto : ConfigurationBase
{
    [Id(0)]
    public string ConsumerKey { get; set; } = "YOUR_TWITTER_API_KEY";
    
    [Id(1)]
    public string ConsumerSecret { get; set; } = "YOUR_API_SECRET";
    
    [Id(2)]
    public string EncryptionPassword { get; set; } = "YOUR_ENCRYPTION_PASSWORD";
    
    [Id(3)]
    public string BearerToken { get; set; } = "YOUR_BEARER_TOKEN";
    
    [Id(4)]
    public int ReplyLimit { get; set; } = 10;
}