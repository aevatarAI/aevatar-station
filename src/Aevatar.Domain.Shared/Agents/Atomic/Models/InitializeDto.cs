using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Atomic.Models;

[GenerateSerializer]
public class InitializeDto : InitializeDtoBase
{
    [Id(0)] public string Properties { get; set; }
}