using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.Agent.GEvents;

[GenerateSerializer]
public class TweetSEvent: StateLogEventBase<TweetSEvent>
{
    [Id(0)] public string Text { get; set; }
}

[GenerateSerializer]
public class TweetRequestSEvent : TweetSEvent
{
    [Id(0)] public Guid RequestId { get; set; }
}

[GenerateSerializer]
public class TweetSocialResponseSEvent : TweetSEvent
{
    [Id(0)] public Guid ResponseId { get; set; }
}