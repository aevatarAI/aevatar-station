using System.Collections.Generic;

namespace Aevatar.Sandbox.Abstractions.Contracts;

public sealed class SandboxExecutionRequest
{
    public required string SandboxExecutionId { get; init; }
    public required string LanguageId { get; init; }
    public required string Code { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public IDictionary<string, string>? Parameters { get; init; }
    public string? TenantId { get; init; }
    public string? ChatId { get; init; }
}