using Orleans;

namespace Aevatar.GAgents.Twitter.Agent.GEvents;

[GenerateSerializer]
public class ReplyTweetSEvent : TweetSEvent
{
    [Id(0)] public string TweetId { get; set; }
    [Id(1)] public string Text { get; set; }
}