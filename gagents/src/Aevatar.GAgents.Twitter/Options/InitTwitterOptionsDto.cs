using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.Twitter.Options;

[GenerateSerializer]
public class InitTwitterOptionsDto : ConfigurationBase
{
    [Id(0)]
    [Required(ErrorMessage = "Consumer Key is required")]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Consumer Key can only contain alphanumeric characters")]
    public string ConsumerKey { get; set; } = "YOUR_TWITTER_API_KEY";
    
    [Id(1)]
    [Required(ErrorMessage = "Consumer Secret is required")]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Consumer Secret can only contain alphanumeric characters")]
    public string ConsumerSecret { get; set; } = "YOUR_API_SECRET";
    
    [Id(2)]
    [Required(ErrorMessage = "Encryption Password is required")]
    public string EncryptionPassword { get; set; } = "YOUR_ENCRYPTION_PASSWORD";
    
    [Id(3)]
    [Required(ErrorMessage = "Bearer Token is required")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Bearer Token format is invalid")]
    public string BearerToken { get; set; } = "YOUR_BEARER_TOKEN";
    
    [Id(4)]
    public int ReplyLimit { get; set; } = 10;
}