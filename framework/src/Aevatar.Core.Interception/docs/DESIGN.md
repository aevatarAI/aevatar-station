# Aevatar.Core.Interception Design Document

## Overview

The Aevatar.Core.Interception library provides comprehensive method interception capabilities for the Aevatar framework using Fody MethodDecorator. It enables automatic tracing, logging, and monitoring of method executions across all access levels and method types.

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────────┐
│                    Aevatar.Core.Interception                    │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌────────────────────────────────────┐  │
│  │ InterceptorAttribute │ ◄──► │ Fody MethodDecorator        │  │
│  │                 │    │                                    │  │
│  │ - Init()        │    │ - IL Weaving                       │  │
│  │ - OnEntry()     │    │ - Runtime Hooks                    │  │
│  │ - OnExit()      │    │                                    │  │
│  │ - OnException() │    │                                    │  │
│  │ - OnTaskCont... │    │                                    │  │
│  └─────────────────┘    └────────────────────────────────────┘  │
│           ▲                                                     │
│           │                                                     │
│  ┌─────────────────────────────────────────────────────┐        │
│  │                 Target Methods                      │        │
│  │ - Public/Private/Protected/Internal                 │        │
│  │ - Static/Instance                                   │        │
│  │ - Sync/Async                                        │        │
│  │ - Generic                                           │        │
│  │ - Constructors                                      │        │
│  │ - Orleans Grains                                    │        │
│  └─────────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **Non-Intrusive**: Uses compile-time IL weaving to avoid runtime performance overhead
2. **Comprehensive Coverage**: Supports all method types and access levels
3. **Orleans Integration**: Seamlessly integrates with Orleans grain lifecycle
4. **Flexible Logging**: Supports multiple logging backends (Console, ILogger, structured logging)
5. **Exception Safe**: Robust exception handling without affecting original method behavior
6. **Async-Aware**: Proper support for async/await patterns with OnTaskContinuation

## Component Details

### InterceptorAttribute

The core attribute that implements the IMethodDecorator interface from Fody MethodDecorator.

#### Key Features

- **Method Lifecycle Hooks**: Entry, Exit, Exception, Task Continuation
- **Parameter Capture**: Automatic parameter name and value capture
- **Logger Integration**: Automatic logger discovery from target instances
- **Context Preservation**: Maintains method context throughout execution
- **Performance Optimized**: Minimal overhead through compile-time weaving

#### Method Flow

```
Method Call
    ↓
1. Init(instance, method, args) - Initialize interceptor context
    ↓
2. OnEntry() - Log method entry and parameters
    ↓
3. [Original Method Execution]
    ↓
4a. OnExit() - Log successful completion
4b. OnException(ex) - Log exception details
4c. OnTaskContinuation(task) - Log async completion
```

### Supported Method Types

| Method Type | Support Level | Notes |
|-------------|---------------|-------|
| **Public Methods** | ✅ Full | Standard method interception |
| **Private Methods** | ✅ Full | IL weaving works at all access levels |
| **Protected Methods** | ✅ Full | Virtual method support |
| **Internal Methods** | ✅ Full | Assembly-level access |
| **Static Methods** | ✅ Full | Class-level operations |
| **Constructors** | ✅ Full | Object initialization tracking |
| **Async Methods** | ✅ Full | Task/ValueTask support with OnTaskContinuation |
| **Generic Methods** | ✅ Full | Type parameter resolution |
| **Extension Methods** | ✅ Full | Static extension method support |
| **Orleans Grains** | ✅ Full | Lifecycle and business method tracking |

### Comprehensive Parameter Type Support

The interceptor provides extensive parameter type coverage for comprehensive tracing and debugging:

#### **Basic Types**
- **Primitives**: `string`, `int`, `bool`, `Guid`, `CancellationToken`
- **Numeric**: `byte`, `sbyte`, `short`, `ushort`, `long`, `ulong`, `float`, `double`, `decimal`, `char`
- **Date/Time**: `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`

#### **Complex Types**
- **Nullable**: `int?`, `string?`, `bool?`, `DateTime?`, `Guid?`
- **Enums**: Built-in enums (`DayOfWeek`, `ConsoleColor`) and custom enums
- **Structs**: Built-in structs (`Guid`, `TimeSpan`) and custom structs

#### **Collection Types**
- **Arrays**: `byte[]`, `int[]`, `string[]`
- **Generic Collections**: `List<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`
- **Non-Generic Interfaces**: `System.Collections.IEnumerable`, `System.Collections.ICollection`, `System.Collections.IList`
- **Generic Interfaces**: `IEnumerable<T>`, `ICollection<T>`, `IList<T>`

#### **Advanced Types**
- **Delegates**: `Action<T>`, `Func<T, TResult>`, custom delegates
- **Special .NET**: `object`, `dynamic`, `IntPtr`, `UIntPtr`
- **Mixed Complex**: Nested generics, tuples, complex type combinations

#### **Parameter Capture Strategy**

The interceptor uses a sophisticated parameter capture strategy:

1. **Type-Aware Serialization**: Each parameter type has optimized serialization logic
2. **Null Safety**: Proper handling of null values and nullable types
3. **Collection Detection**: Smart detection of collection types for meaningful logging
4. **Generic Resolution**: Proper handling of generic type parameters and constraints
5. **Performance Optimization**: Minimal overhead through compile-time weaving

### Logging Strategy

#### Logger Discovery Priority

1. **Instance ILogger**: If target instance implements ILogger
2. **Instance Logger Property**: If target has a "Logger" property of type ILogger
3. **Console Fallback**: Console.WriteLine for basic logging
4. **Structured Logging**: Support for additional context and correlation

#### Log Format

```
Entry: TRACE: Entering {MethodName}
Parameters: TRACE: Parameter {ParamName} = {ParamValue}
Exit: TRACE: Exiting {MethodName}
Exception: TRACE: Exception in {MethodName}: {ExceptionType}: {ExceptionMessage}
Async: TRACE: Async completed/exception {MethodName}
```

## Integration Guide

### Step 1: Enable Module-Level Interception

Add `[module: Interceptor]` to each project/DLL/assembly where you want tracing capabilities:

```csharp
// Add this line to any .cs file in your project (typically GlobalUsings.cs or AssemblyInfo.cs)
[module: Interceptor]

namespace MyNamespace
{
    // All methods in this assembly now have interception capability
    public class MyService
    {
        public void DoWork() { /* can be traced */ }
        private void HelperMethod() { /* can be traced */ }
    }
}
```

### Step 2: Apply Method-Level Tracing

Apply `[Interceptor]` attribute on specific methods that need to be traced (works for any class type):

```csharp
// Regular service class
public class MyService
{
    [Interceptor]
    public void CriticalMethod() { /* traced */ }
    
    public void RegularMethod() { /* not traced */ }
    
    [Interceptor]
    private async Task ProcessDataAsync() { /* traced */ }
}

// Orleans grain class
[GAgent]
public class MyGAgent : GAgentBase<MyState, MyEvent>
{
    [Interceptor]
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Lifecycle tracking
        await base.OnActivateAsync(cancellationToken);
    }
    
    [Interceptor]
    public async Task ProcessEventAsync(MyEvent evt)
    {
        // Business method tracking
        await HandleBusinessLogic(evt);
    }
}
```

### Step 3: Runtime Trace Activation

The above steps enable tracing capability during build. To actually activate tracing at runtime:

#### For HTTP Requests (Client to Agent)
```bash
# 1. Add trace ID to tracked list via admin API
POST /api/tracked-traces
{
  "TraceId": "user-session-123"
}

# 2. Include trace ID in any API call headers
curl -H "X-Trace-Id: user-session-123" \
     -H "Content-Type: application/json" \
     -d '{"agentType": "testdbgagent", "name": "test-agent-001"}' \
     http://localhost/api/agent

# The middleware will extract the trace ID and enable tracing for the request
```

#### For Direct Agent Calls (Client to Agent)
```csharp
// Set RequestContext properly for Orleans grain calls
RequestContext.Set("ActiveTraceId", "user-session-123");
RequestContext.Set("TraceConfig", new TraceConfig 
{ 
    Enabled = true, 
    TrackedIds = { "user-session-123" } 
});

// Now call your agent - tracing will be active
var result = await myAgent.ProcessEventAsync(eventData);
```

#### Trace ID Propagation
- **HTTP → Agent**: Trace ID automatically propagates through middleware to Orleans context
- **Agent → Agent**: Trace ID propagates through Orleans RequestContext between grain calls
- **Runtime Control**: Use `ITraceManager` service for dynamic trace ID management

### Important Notes

- **Build-Time Setup**: `[module: Interceptor]` and `[Interceptor]` enable tracing capability
- **Runtime Activation**: Trace IDs must be added to tracked list and included in requests
- **Zero Overhead**: Methods without active trace IDs have minimal performance impact
- **Selective Control**: Enable tracing only for specific requests/operations using trace IDs

## Performance Considerations

### Compile-Time Weaving

- **Zero Runtime Overhead**: IL weaving happens at compile time
- **No Reflection**: Direct method calls, no reflection-based interception
- **Minimal IL Expansion**: Efficient instruction injection

### Runtime Performance

- **Parameter Capture**: Only active when parameters exist
- **Logger Caching**: Logger discovery cached per instance
- **Exception Handling**: Try-catch blocks only when necessary
- **Async Optimization**: OnTaskContinuation only for async methods

### Memory Impact

- **Instance Fields**: 4 fields per intercepted method instance
- **String Allocations**: Parameter values converted to strings on-demand
- **Logger References**: Weak references to avoid memory leaks

## Configuration

### Fody Configuration

```xml
<!-- FodyWeavers.xml -->
<?xml version="1.0" encoding="utf-8"?>
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
         xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <MethodDecorator />
</Weavers>
```

### Attribute Targets

```csharp
[AttributeUsage(AttributeTargets.Method | 
                AttributeTargets.Constructor | 
                AttributeTargets.Assembly | 
                AttributeTargets.Module)]
```

## Error Handling

### Exception Safety

- **Original Behavior Preserved**: Exceptions from original methods are re-thrown
- **Interceptor Isolation**: Interceptor exceptions don't affect original method
- **Logging Failures**: Fallback to console if logging fails
- **Async Exception Handling**: OnTaskContinuation handles async exceptions

### Failure Modes

| Failure Scenario | Behavior | Recovery |
|------------------|----------|----------|
| Logger Not Found | Console fallback | Continue execution |
| Parameter Serialization Fails | Log "serialization failed" | Continue execution |
| Interceptor Exception | Log error to console | Continue execution |
| IL Weaving Fails | Compile-time error | Fix configuration |

## Testing Strategy

### Unit Testing

- **Interceptor Behavior**: Test each lifecycle method
- **Parameter Capture**: Verify parameter logging accuracy
- **Exception Handling**: Test exception propagation and logging
- **Logger Integration**: Test logger discovery and fallback
- **Method Types**: Test all supported method types

### Integration Testing

- **Orleans Integration**: Test grain lifecycle interception
- **Module-Level Application**: Test assembly-wide interception
- **Performance Impact**: Measure overhead of interception
- **Memory Usage**: Test for memory leaks and retention

### End-to-End Testing

- **Real Grain Scenarios**: Test actual Orleans grain workflows
- **Complex Call Chains**: Test nested method calls
- **Async Patterns**: Test various async/await scenarios
- **Exception Scenarios**: Test exception propagation through call stacks

## Dependencies

### Required Packages

- **Fody**: IL weaving framework
- **MethodDecorator.Fody**: Method interception support
- **Microsoft.Extensions.Logging.Abstractions**: Logging interface support

### Optional Dependencies

- **Microsoft.Orleans.Core**: Orleans grain integration
- **Microsoft.Extensions.Logging**: Enhanced logging support
- **System.Text.Json**: JSON serialization for structured logging

## Compatibility

### Framework Support

- **.NET 9.0**: Primary target framework
- **.NET 8.0**: Compatible
- **.NET Standard 2.1**: Compatible for library consumption

### Orleans Versions

- **Orleans 9.0.1**: Fully supported

## Security Considerations

### Production Usage Guidelines

- **User Consent Required**: Always obtain user consent before enabling tracing in production environments
- **Troubleshooting Only**: Tracing should only be used for troubleshooting and debugging purposes
- **Data Privacy**: Be aware that tracing captures method parameters and execution details
- **Limited Duration**: Enable tracing only for the minimum time necessary to resolve issues

### Parameter Logging

- **Sensitive Data**: No automatic filtering of sensitive parameters
- **PII Concerns**: Consider parameter content when enabling interception
- **Audit Trails**: Interception creates detailed audit trails

### Recommendations

1. **Selective Application**: Apply interception selectively to avoid logging sensitive data
2. **Custom Serializers**: Implement custom parameter serializers for sensitive types
3. **Log Retention**: Configure appropriate log retention policies
4. **Access Controls**: Secure access to intercepted logs
