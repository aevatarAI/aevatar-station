using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;

[GenerateSerializer]
public class SocialGEvent:EventWithResponseBase<SocialResponseGEvent>
{
    [Id(0)] public Guid RequestId { get; set; } = Guid.NewGuid();
    
    [Description("The content of the chat.")]
    [Id(1)] public string Content { get; set; }
    [Id(2)]  public string MessageId { get; set; }
    [Description("Unique identifier for the chat from which the message was received.")]
    [Id(3)]  public string ChatId { get; set; }
}