using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Twitter.Agent.GEvents;
using Aevatar.GAgents.Twitter.Options;
using Orleans;

namespace Aevatar.GAgents.Twitter.Agent;

[GenerateSerializer]
public class TwitterGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string UserId { get; set; }
    [Id(2)] public string Token { get; set; }
    [Id(3)] public string TokenSecret { get; set; }
    [Id(4)] public Dictionary<string, string> RepliedTweets { get; set; } = new Dictionary<string, string>();
    [Id(5)] public string UserName { get; set; }
    [Id(6)] public List<Guid> SocialRequestList { get; set; } = new List<Guid>();
    [Id(8)] public string ConsumerKey { get; set; }
    [Id(9)] public string ConsumerSecret { get; set; }
    [Id(10)] public string EncryptionPassword { get; set; }
    [Id(11)] public string BearerToken { get; set; }
    [Id(12)] public int ReplyLimit { get; set; }
}