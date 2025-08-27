using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class ResponseTestEvent : EventWithResponseBase<NaiveTestEvent>
{
    [Id(0)] public string Greeting { get; set; }
}

[GenerateSerializer]
public class AnotherResponseTestEvent : EventWithResponseBase<NaiveTestEvent>
{
    [Id(0)] public string Greeting { get; set; }
}