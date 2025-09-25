using System;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using Orleans;

namespace Aevatar.Core.Interception.E2E.Tests.TestGrains;

/// <summary>
/// Simple test grain for testing trace context propagation in Orleans E2E tests.
/// </summary>
public interface ITraceTestGrain : IGrainWithGuidKey
{
    Task<string> GetTraceIdAsync();
    Task<TraceConfig?> GetTraceConfigAsync();
    Task<string> CallOtherGrainAsync(Guid otherGrainId);
    Task<string> GetTraceIdFromRequestContextAsync();
}

public class TraceTestGrain : GAgentBase<TraceTestState, TraceTestStateLogEvent>, ITraceTestGrain
{
    public async Task<string> GetTraceIdAsync()
    {
        // Return the current trace ID from TraceContext
        return TraceContext.ActiveTraceId ?? "no-trace-id";
    }

    public async Task<TraceConfig?> GetTraceConfigAsync()
    {
        // Return the current trace configuration
        return TraceContext.GetTraceConfig();
    }

    public async Task<string> CallOtherGrainAsync(Guid otherGrainId)
    {
        // Call another grain to test trace context propagation
        var otherGrain = GrainFactory.GetGrain<ITraceTestGrain>(otherGrainId);
        return await otherGrain.GetTraceIdAsync();
    }

    public async Task<string> GetTraceIdFromRequestContextAsync()
    {
        // This would normally read from Orleans RequestContext
        // For testing purposes, we'll return the current TraceContext value
        return TraceContext.ActiveTraceId ?? "no-trace-id";
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("TraceTestGrain for E2E testing");
    }
}

// Simple state and log event classes for the test grain
public class TraceTestState : StateBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
}

public class TraceTestStateLogEvent : StateLogEventBase<TraceTestStateLogEvent>
{
    public string Message { get; set; } = "Test event";
}
