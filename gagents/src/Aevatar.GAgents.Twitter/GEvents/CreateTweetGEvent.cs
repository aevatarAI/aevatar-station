using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GEvents;

[Description("create a tweet")]
[GenerateSerializer]
public class CreateTweetGEvent:EventBase
{
    [Description("text content to be post")]
    [Id(0)]  public string Text { get; set; }
}