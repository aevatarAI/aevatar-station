using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GEvents;

[Description("Receive a reply from tweet.")]
[GenerateSerializer]
public class ReceiveReplyGEvent:EventBase
{
    [Description("Unique identifier for the tweet which got replied.")]
    [Id(0)]  public string TweetId { get; set; }
    [Description("Text content of the reply.")]
    [Id(1)] public string Text { get; set; }
}