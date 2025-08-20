using System;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Orleans;

namespace Aevatar.Sandbox.Abstractions.Grains;

public interface ISandboxExecutionClientGrain : IGrainWithGuidKey
{
    Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionClientParams @params);
}

[GenerateSerializer]
public sealed class SandboxExecutionClientParams
{
    [Id(0)] public required string LanguageId { get; init; }
    [Id(1)] public required string Code { get; init; }
    [Id(2)] public int TimeoutSeconds { get; init; } = 30;
    [Id(3)] public string? TenantId { get; init; }
    [Id(4)] public string? ChatId { get; init; }
}