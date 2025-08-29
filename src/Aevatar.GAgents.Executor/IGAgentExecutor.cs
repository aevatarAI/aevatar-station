using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Executor;

public interface IGAgentExecutor
{
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event, Type? expectedResultType = null);

    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, string eventTypeName, string eventJson,
        Type? expectedResultType = null);

    Task<string> ExecuteGAgentEventHandler(GrainId grainId, string eventTypeName, string eventJson,
        Type? expectedResultType = null);

    Task<string> ExecuteGAgentEventHandler(GrainType grainType, string eventTypeName, string eventJson,
        Type? expectedResultType = null);
}