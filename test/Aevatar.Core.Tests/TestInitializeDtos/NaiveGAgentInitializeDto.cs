using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestInitializeDtos;

[GenerateSerializer]
public class NaiveGAgentInitialize : InitializationEventBase
{
    [Id(0)] public string InitialGreeting { get; set; }
}