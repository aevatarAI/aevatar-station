using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class DevelopTaskTestEvent : EventWithResponseBase<NewFeatureCompletedTestEvent>
{
    [Id(0)] public string Description { get; set; }
}