using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class NewFeatureCompletedTestEvent : EventBase
{
    [Id(0)] public string PullRequestUrl { get; set; }
}