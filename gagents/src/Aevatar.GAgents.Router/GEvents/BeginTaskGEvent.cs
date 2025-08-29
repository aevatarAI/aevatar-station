using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Router.GEvents;

[Description("Begin a new task and trigger Router")]
[GenerateSerializer]
public class BeginTaskGEvent : EventBase
{
    [Description("task described by natural language")]
    [Id(0)] public string TaskDescription { get; set; }
}