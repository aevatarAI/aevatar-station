// ABOUTME: This file implements the base class for PsiOmni agents with comprehensive event tracing
// ABOUTME: Provides configurable logging for event sourcing, incoming/outgoing events, and internal method calls

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using GroupChat.GAgent;
using GroupChat.GAgent.GEvent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.PsiOmni;

public abstract class PsiOmniAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GroupMemberGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : PsiOmniGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : PsiOmniGAgentConfig
{
    private bool _enableEventTracing = true;
    private string _eventTracingLoggerName = string.Empty;
    private ILogger? _eventLogger;
    private IDisposable? _loggingScope;
    protected string AgentId { get; private set; } = string.Empty;
    protected string AgentType { get; private set; } = string.Empty;
    protected string SessionId { get; private set; } = string.Empty;

    protected PsiOmniAgentBase()
    {
        AgentType = GetType().Name;
    }

    protected void InitializeTracing()
    {
        try
        {
            // Initialize logger if not already done
            if (_eventLogger == null)
            {
                _eventTracingLoggerName = $"PsiOmni.EventTracing.{GetType().Name}";
                _eventLogger = Logger; // Use the logger from the Orleans grain base class

                Console.WriteLine(
                    $"[DEBUG] PsiOmni EventTracing - Enabled: {_enableEventTracing}, Logger: {_eventTracingLoggerName}");
            }

            if (_enableEventTracing && _eventLogger != null)
            {
                // Get the agent ID from the primary key or grain ID as fallback
                if (string.IsNullOrEmpty(AgentId))
                {
                    AgentId = this.GetGrainId().ToString();
                    Console.WriteLine($"[DEBUG] Setting AgentId: {AgentId}");
                }

                // Initialize SessionId if not already set
                if (string.IsNullOrEmpty(SessionId))
                {
                    SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
                }

                // Create or recreate logging scope if needed
                if (_loggingScope == null)
                {
                    _loggingScope = _eventLogger.BeginScope(new Dictionary<string, object>
                    {
                        ["AgentId"] = AgentId,
                        ["AgentType"] = AgentType,
                        ["SessionId"] = SessionId,
                        ["ActivationId"] = this.GetGrainId().ToString()
                    });

                    // Force a test log to verify logger is working
                    _eventLogger.LogInformation("TEST: PsiOmni event tracing is enabled and working! AgentId={AgentId}",
                        AgentId);
                    Console.WriteLine($"[DEBUG] Test log sent to event logger for agent {AgentId}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to initialize PsiOmni event tracing: {ex.Message}");
            _enableEventTracing = false;
        }
    }

    protected void DisposeTracing()
    {
        if (_enableEventTracing)
        {
            LogEventTrace("Agent tracing disposing");
            _loggingScope?.Dispose();
        }
    }

    #region Event Sourcing Layer Tracing

    protected void RaiseEventWithTracing(TStateLogEvent @event)
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and scope exists
            InitializeTracing();

            var eventId = GetEventId(@event);
            var correlationId = GetOrCreateCorrelationId();

            LogEventDebug(
                "RaiseEvent called: EventType={EventType}, EventId={EventId}, CorrelationId={CorrelationId}",
                @event.GetType().Name,
                eventId,
                correlationId
            );
        }

        RaiseEvent(@event);

        if (_enableEventTracing)
        {
            LogEventDebug(
                "RaiseEvent completed"
            );
        }
    }

    protected async Task ConfirmEventsWithTracing()
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and scope exists
            InitializeTracing();

            var pendingCount = GetPendingEventCount();
            LogEventDebug(
                "ConfirmEvents called: PendingEvents={PendingCount}",
                pendingCount
            );
        }

        var stopwatch = Stopwatch.StartNew();
        await ConfirmEvents();

        if (_enableEventTracing)
        {
            LogEventDebug(
                "ConfirmEvents completed: Duration={Duration}ms",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    #endregion

    #region Event Handler Tracing

    protected async Task<T> TraceEventHandlerAsync<T>(EventBase @event, Func<Task<T>> handler,
        [CallerMemberName] string handlerName = "")
    {
        if (!_enableEventTracing)
        {
            return await handler();
        }

        // Make sure tracing is initialized and scope exists
        InitializeTracing();

        var eventId = GetEventId(@event);
        var correlationId = GetOrCreateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        using (LogEventScope(new Dictionary<string, object>
               {
                   ["EventType"] = @event.GetType().Name,
                   ["EventId"] = eventId,
                   ["Handler"] = handlerName,
                   ["CorrelationId"] = correlationId
               }))
        {
            LogEventInfo(
                "Event handler started: EventType={EventType}, EventId={EventId}, Handler={Handler}",
                @event.GetType().Name,
                eventId,
                handlerName
            );

            try
            {
                var result = await handler();

                LogEventInfo(
                    "Event handler completed: EventId={EventId}, Duration={Duration}ms",
                    eventId,
                    stopwatch.ElapsedMilliseconds
                );

                return result;
            }
            catch (Exception ex)
            {
                LogEventError(
                    ex,
                    "Event handler failed: EventId={EventId}, Duration={Duration}ms, Error={Error}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message
                );
                throw;
            }
        }
    }

    protected Task TraceEventHandlerAsync(EventBase @event, Func<Task> handler,
        [CallerMemberName] string handlerName = "")
    {
        return TraceEventHandlerAsync(@event, async () =>
        {
            await handler();
            return true;
        }, handlerName);
    }

    #endregion

    #region Outgoing Event/Message Tracing

    protected async Task PublishAsyncToSelfWithTracing(TEvent @event)
    {
        await PublishAsyncWithTracing(this.GetGrainId(), @event);
    }

    protected async Task PublishAsyncWithTracing(GrainId grainId, TEvent @event)
    {
        if (_enableEventTracing)
        {
            var eventId = GetEventId(@event);
            var correlationId = GetOrCreateCorrelationId();

            LogEventInfo(
                "Publishing event: SourceAgent={SourceAgent}, EventType={EventType}, EventId={EventId}, CorrelationId={CorrelationId}",
                AgentId,
                @event.GetType().Name,
                eventId,
                correlationId
            );
        }

        await PublishAsync(grainId, @event);

        if (_enableEventTracing)
        {
            LogEventDebug("Event published successfully: EventId={EventId}", GetEventId(@event));
        }
    }

    #endregion

    #region Internal Method Tracing

    protected async Task<T> TraceMethodAsync<T>(Func<Task<T>> method, object? parameters = null,
        [CallerMemberName] string methodName = "")
    {
        if (!_enableEventTracing)
        {
            return await method();
        }

        // Make sure tracing is initialized and scope exists
        InitializeTracing();

        var stopwatch = Stopwatch.StartNew();
        var methodId = Guid.NewGuid().ToString("N").Substring(0, 8);

        LogMethodEntry(methodName, methodId, parameters);

        try
        {
            var result = await method();
            LogMethodExit(methodName, methodId, result, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            LogMethodError(methodName, methodId, ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    protected Task TraceMethodAsync(Func<Task> method, object? parameters = null,
        [CallerMemberName] string methodName = "")
    {
        return TraceMethodAsync(async () =>
        {
            await method();
            return true;
        }, parameters, methodName);
    }

    #endregion

    #region Logging Helpers

    protected void LogEventTrace(string message, params object[] args)
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and agent ID is available
            InitializeTracing();

            // Ensure AgentId is included in the log context
            if (string.IsNullOrEmpty(AgentId))
            {
                AgentId = this.GetGrainId().ToString();
            }

            // Create a new temporary scope for this log call instead of using the existing one
            using (var tempScope = _eventLogger?.BeginScope(new Dictionary<string, object>
                   {
                       ["AgentId"] = AgentId,
                       ["AgentType"] = AgentType
                   }))
            {
                // Let the scope handle AgentId and AgentType
                _eventLogger?.LogInformation(message, args);
            }
        }
    }

    protected void LogEventInfo(string message, params object[] args)
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and agent ID is available
            InitializeTracing();

            // Ensure AgentId is included in the log context
            if (string.IsNullOrEmpty(AgentId))
            {
                AgentId = this.GetGrainId().ToString();
            }

            // Create a new temporary scope for this log call instead of using the existing one
            using (var tempScope = _eventLogger?.BeginScope(new Dictionary<string, object>
                   {
                       ["AgentId"] = AgentId,
                       ["AgentType"] = AgentType
                   }))
            {
                // Let the scope handle AgentId and AgentType
                _eventLogger?.LogInformation(message, args);
            }
        }
    }

    private void EnsureLoggingScopeExists()
    {
        if (_loggingScope == null && !string.IsNullOrEmpty(AgentId) && _eventLogger != null)
        {
            _loggingScope = _eventLogger.BeginScope(new Dictionary<string, object>
            {
                ["AgentId"] = AgentId,
                ["AgentType"] = AgentType,
                ["SessionId"] = SessionId ?? Guid.NewGuid().ToString("N").Substring(0, 8),
                ["ActivationId"] = this.GetGrainId().ToString()
            });

            Console.WriteLine($"[DEBUG] Recreated logging scope for agent {AgentId}");
        }
    }

    protected void LogEventDebug(string message, params object[] args)
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and agent ID is available
            InitializeTracing();

            // Ensure AgentId is included in the log context
            if (string.IsNullOrEmpty(AgentId))
            {
                AgentId = this.GetGrainId().ToString();
            }

            // Create a new temporary scope for this log call instead of using the existing one
            using (var tempScope = _eventLogger?.BeginScope(new Dictionary<string, object>
                   {
                       ["AgentId"] = AgentId,
                       ["AgentType"] = AgentType
                   }))
            {
                // Let the scope handle AgentId and AgentType
                _eventLogger?.LogDebug(message, args);
            }
        }
    }

    protected void LogEventError(Exception ex, string message, params object[] args)
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and agent ID is available
            InitializeTracing();

            // Ensure AgentId is included in the log context
            if (string.IsNullOrEmpty(AgentId))
            {
                AgentId = this.GetGrainId().ToString();
            }

            // Create a new temporary scope for this log call instead of using the existing one
            using (var tempScope = _eventLogger?.BeginScope(new Dictionary<string, object>
                   {
                       ["AgentId"] = AgentId,
                       ["AgentType"] = AgentType
                   }))
            {
                // Let the scope handle AgentId and AgentType
                _eventLogger?.LogError(ex, message, args);
            }
        }
    }

    protected IDisposable? LogEventScope(Dictionary<string, object> scopeData)
    {
        if (_enableEventTracing)
        {
            // Make sure tracing is initialized and agent ID is available
            InitializeTracing();

            // Ensure AgentId is included in the scope data
            if (!string.IsNullOrEmpty(AgentId) && !scopeData.ContainsKey("AgentId"))
            {
                scopeData["AgentId"] = AgentId;
            }

            // Ensure AgentType is included in the scope data
            if (!string.IsNullOrEmpty(AgentType) && !scopeData.ContainsKey("AgentType"))
            {
                scopeData["AgentType"] = AgentType;
            }

            return _eventLogger?.BeginScope(scopeData);
        }

        return null;
    }

    private void LogMethodEntry(string methodName, string methodId, object? parameters)
    {
        if (parameters != null)
        {
            var parametersJson = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 2
            });
            LogEventDebug(
                "Method entry: Method={Method}, MethodId={MethodId}, Parameters={Parameters}",
                methodName,
                methodId,
                parametersJson.Length > 200 ? parametersJson.Substring(0, 200) + "..." : parametersJson
            );
        }
        else
        {
            LogEventDebug(
                "Method entry: Method={Method}, MethodId={MethodId}",
                methodName,
                methodId
            );
        }
    }

    private void LogMethodExit(string methodName, string methodId, object? result, long durationMs)
    {
        LogEventDebug(
            "Method exit: Method={Method}, MethodId={MethodId}, Duration={Duration}ms",
            methodName,
            methodId,
            durationMs
        );
    }

    private void LogMethodError(string methodName, string methodId, Exception ex, long durationMs)
    {
        LogEventError(
            ex,
            "Method failed: Method={Method}, MethodId={MethodId}, Duration={Duration}ms, Error={Error}",
            methodName,
            methodId,
            durationMs,
            ex.Message
        );
    }

    #endregion

    #region Helper Methods

    private string GetEventId(object @event)
    {
        // Try to get Id property
        var idProperty = @event.GetType().GetProperty("Id");
        if (idProperty != null)
        {
            var id = idProperty.GetValue(@event);
            if (id != null)
                return id.ToString() ?? "no-id";
        }

        // Try to get UniqueId property (for PsiOmni events)
        var uniqueIdProperty = @event.GetType().GetProperty("UniqueId");
        if (uniqueIdProperty != null)
        {
            var uniqueId = uniqueIdProperty.GetValue(@event);
            if (uniqueId != null)
                return uniqueId.ToString() ?? "no-id";
        }

        // Generate a unique ID if none exists
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private string GetOrCreateCorrelationId()
    {
        // You could implement correlation ID tracking here
        // For now, return a new ID
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private int GetPendingEventCount()
    {
        // This is a placeholder - actual implementation would depend on GAgentBase internals
        return 0;
    }

    #endregion

    #region Diagnostic Methods

    public virtual Task<PsiOmniAgentDiagnostics> GetDiagnosticsAsync()
    {
        return Task.FromResult(new PsiOmniAgentDiagnostics
        {
            AgentId = AgentId,
            AgentType = AgentType,
            SessionId = SessionId,
            StateVersion = 0, // Version tracking not available in this state type
            RealizationStatus = State.RealizationStatus.ToString(),
            ChildAgentCount = State.ChildAgents.Count,
            ChatHistoryLength = State.ChatHistory.Count,
            TodoCount = State.TodoList.Count,
            LastProcessedEventId = State.CallId,
            TracingEnabled = _enableEventTracing
        });
    }

    #endregion

    #region Publishing

    private async Task PublishAsync<T>(GrainId grainId, T @event) where T : EventBase
    {
        var grainIdString = grainId.ToString();
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace, grainIdString);
        var stream = StreamProvider.GetStream<EventWrapperBase>(streamId);
        var eventWrapper = new EventWrapper<T>(@event, Guid.NewGuid(), this.GetGrainId());
        await stream.OnNextAsync(eventWrapper);
    }

    #endregion Publishing
}

[GenerateSerializer]
public class PsiOmniAgentDiagnostics
{
    [Id(0)] public string AgentId { get; set; } = string.Empty;
    [Id(1)] public string AgentType { get; set; } = string.Empty;
    [Id(2)] public string SessionId { get; set; } = string.Empty;
    [Id(3)] public int StateVersion { get; set; }
    [Id(4)] public string RealizationStatus { get; set; } = string.Empty;
    [Id(5)] public int ChildAgentCount { get; set; }
    [Id(6)] public int ChatHistoryLength { get; set; }
    [Id(7)] public int TodoCount { get; set; }
    [Id(8)] public string LastProcessedEventId { get; set; } = string.Empty;
    [Id(9)] public bool TracingEnabled { get; set; }
}