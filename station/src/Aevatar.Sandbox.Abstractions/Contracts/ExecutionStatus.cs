using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public enum ExecutionStatus
{
    [Id(0)]
    Pending,

    [Id(1)]
    Running,

    [Id(2)]
    Completed,

    [Id(3)]
    Failed,

    [Id(4)]
    TimedOut,

    [Id(5)]
    Cancelled
}