using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Router.GEvents;

[Description("Inform router GAgent to determine the following agent and event")]
[GenerateSerializer]
public class RouteNextGEvent : EventBase
{
    [Description("Process result of previous event")]
    [Id(0)] public string ProcessResult { get; set; }
}