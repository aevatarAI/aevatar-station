# PsiOmni Agent Event Tracing

This document explains how to use the comprehensive event tracing system added to PsiOmni agents for debugging event delivery and processing issues.

## Overview

The tracing system provides visibility into:
- **Event Sourcing Layer**: StateLogEvent creation, RaiseEvent calls, ConfirmEvents operations
- **Incoming Events**: Event handler execution, duplicate detection, processing status
- **Outgoing Events**: Message publishing, agent calls, broadcast operations
- **Internal Methods**: Method execution, duration tracking, error handling
- **State Transitions**: State changes, agent realization, child agent management

## Configuration

### Enable/Disable Tracing

Add to your `appsettings.json`:

```json
{
  "PsiOmni": {
    "EnableEventTracing": true
  },
  "Logging": {
    "LogLevel": {
      "PsiOmni.EventTracing": "Information"
    }
  }
}
```

### Environment Variable

Set the environment variable:
```bash
export PsiOmni__EnableEventTracing=true
```

### Runtime Toggle

Tracing can be enabled/disabled without code changes by updating configuration and restarting the application.

## Log Categories

The tracing system uses separate logger categories for different purposes:

- `PsiOmni.EventTracing.PsiOmniGAgent` - Main agent events
- `PsiOmni.EventTracing.{AgentType}` - Specific agent type events

## What Gets Logged

### Event Sourcing Layer
```
[10:15:23.456 INF] [PsiOmniGAgent] RaiseEvent called: EventType=ReceiveUserMessageEvent, EventId=msg-123, StateVersion=5
[10:15:23.457 DBG] [PsiOmniGAgent] ConfirmEvents called: PendingEvents=1, StateVersion=5
[10:15:23.459 DBG] [PsiOmniGAgent] ConfirmEvents completed: Duration=2ms, StateVersion=6
```

### Incoming Event Handlers
```
[10:15:23.460 INF] [PsiOmniGAgent] Event handler started: EventType=UserMessageEvent, EventId=msg-123, Handler=HandleUserMessageEventAsync
[10:15:23.465 INF] [PsiOmniGAgent] Event handler completed: EventId=msg-123, Duration=5ms
```

### Outgoing Events/Messages
```
[10:15:23.470 INF] [PsiOmniGAgent] Publishing event: SourceAgent=orchestrator-1, TargetGrain=specialized-agent-2, EventType=UserMessageEvent, EventId=msg-124
[10:15:23.472 DBG] [PsiOmniGAgent] Event published successfully: EventId=msg-124
```

### Internal Method Calls
```
[10:15:23.475 DBG] [PsiOmniGAgent] Method entry: Method=RunAsync, MethodId=run-abc123, Parameters={"trigger":"User Message"}
[10:15:23.680 DBG] [PsiOmniGAgent] Method exit: Method=RunAsync, MethodId=run-abc123, Duration=205ms
```

### State Transitions
```
[10:15:23.481 DBG] [PsiOmniGAgent] State transition started: EventType=ReceiveUserMessageEvent, StateVersion=6
[10:15:23.482 INF] [PsiOmniGAgent] Processing user message: CallId=call-456, ReplyTo=user-agent-1, ContentLength=128
```

### Agent Lifecycle
```
[10:15:20.123 INF] [PsiOmniGAgent] Agent activated: AgentId=orchestrator-1, SessionId=a3f4d5e6
[10:15:35.789 INF] [PsiOmniGAgent] Agent deactivating: Reason=ShutDown
```

## Structured Logging Properties

All trace logs include structured properties for easy querying:

- `AgentId` - Unique identifier for the agent instance
- `AgentType` - Type of the agent (e.g., PsiOmniGAgent)
- `SessionId` - Unique ID for this activation session
- `EventType` - Type of event being processed
- `EventId` - Unique identifier for the event
- `CorrelationId` - ID to correlate related events
- `Duration` - Method/operation execution time

## Querying Logs

### Find all logs from specific agent
```bash
grep "AgentId=orchestrator-1" app.log
```

### Find event flow for specific event
```bash
grep "EventId=msg-123" app.log | sort
```

### Find all orchestrator agents
```bash
grep "AgentType=PsiOmniGAgent" app.log
```

### Using jq for JSON logs
```bash
# Find all events from specific agent
jq 'select(.AgentId == "orchestrator-1")' app.log

# Find specific event flow
jq 'select(.EventId == "msg-123")' app.log

# Find failed operations
jq 'select(.Level == "Error" and .SourceContext | startswith("PsiOmni.EventTracing"))' app.log
```

## Diagnostic Methods

Each agent exposes diagnostic information:

```csharp
var diagnostics = await agent.GetDiagnosticsAsync();
// Returns: AgentId, AgentType, SessionId, StateVersion, RealizationStatus, 
//          ChildAgentCount, ChatHistoryLength, TodoCount, TracingEnabled
```

## Performance Impact

- **When Disabled**: Minimal overhead (simple boolean check)
- **When Enabled**: Small performance impact from logging operations
- **Structured Logging**: Properties are computed only when tracing is enabled

## Troubleshooting Common Issues

### Event Not Being Delivered
1. Check outgoing event logs from sender
2. Check incoming event handler logs from receiver
3. Look for duplicate detection messages
4. Verify target agent IDs match

### Agent Not Processing Events
1. Check agent activation logs
2. Verify state transition logs
3. Look for error logs in event handlers
4. Check ConfirmEvents completion

### State Inconsistencies
1. Review StateLogEvent creation logs
2. Check RaiseEvent and ConfirmEvents pairs
3. Look for failed state transitions
4. Verify event ordering

### Performance Issues
1. Check method duration logs
2. Look for slow ConfirmEvents operations
3. Monitor event handler execution times
4. Check for excessive logging overhead

## Example Trace Flow

Here's what a complete user message flow looks like:

```
[10:15:23.456] Agent activated: AgentId=orchestrator-1
[10:15:23.460] Event handler started: EventType=UserMessageEvent, EventId=msg-123
[10:15:23.461] Processing user message: CallId=call-456, ContentLength=128
[10:15:23.462] RaiseEvent called: EventType=ReceiveUserMessageEvent, StateVersion=5
[10:15:23.463] ConfirmEvents completed: StateVersion=6
[10:15:23.465] Event handler completed: EventId=msg-123, Duration=5ms
[10:15:23.470] Method entry: Method=RunAsync, Parameters={"trigger":"User Message"}
[10:15:23.475] Processing with status: Orchestrator
[10:15:23.480] Creating new agent: CallId=call-789, TaskLength=64
[10:15:23.485] Publishing event: TargetGrain=specialized-agent-2, EventType=UserMessageEvent
[10:15:23.490] Agent created successfully: AgentId=specialized-agent-2
[10:15:23.680] Method exit: Method=RunAsync, Duration=205ms
```

## Best Practices

1. **Enable tracing temporarily** when debugging specific issues
2. **Use structured logging queries** to filter relevant events
3. **Monitor performance impact** in production environments
4. **Correlate events across agents** using correlation IDs
5. **Archive old trace logs** to manage disk space
6. **Use appropriate log levels** (Debug for detailed tracing, Info for key events)

## Configuration Examples

### Development Environment
```json
{
  "PsiOmni": { "EnableEventTracing": true },
  "Logging": {
    "LogLevel": {
      "PsiOmni.EventTracing": "Debug"
    }
  }
}
```

### Production Troubleshooting
```json
{
  "PsiOmni": { "EnableEventTracing": true },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "PsiOmni.EventTracing": "Information"
    }
  }
}
```

### Disabled (Default)
```json
{
  "PsiOmni": { "EnableEventTracing": false },
  "Logging": {
    "LogLevel": {
      "PsiOmni.EventTracing": "None"
    }
  }
}
```