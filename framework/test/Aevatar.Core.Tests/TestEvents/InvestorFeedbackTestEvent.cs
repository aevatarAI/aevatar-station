using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class InvestorFeedbackTestEvent : EventBase
{
    [Id(0)] public string Content { get; set; }
}