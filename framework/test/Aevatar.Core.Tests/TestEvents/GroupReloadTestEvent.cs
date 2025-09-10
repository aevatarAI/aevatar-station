using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[Description("Developer Base Event.")]
[GenerateSerializer]
public class GroupReloadTestEvent : EventBase
{
    [Id(0)] public Guid GroupManagerGuid { get; set; }
}