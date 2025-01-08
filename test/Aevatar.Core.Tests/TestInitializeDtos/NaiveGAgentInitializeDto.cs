using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestInitializeDtos;

[GenerateSerializer]
public class NaiveGAgentInitializeDto : InitializeDtoBase
{
    [Id(0)] public string InitialGreeting { get; set; }
}