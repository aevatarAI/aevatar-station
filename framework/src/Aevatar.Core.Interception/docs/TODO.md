# Aevatar.Core.Interception TODO

## Overview

This document outlines potential future enhancements for the Aevatar.Core.Interception library to provide comprehensive observability, metrics collection, and advanced tracing capabilities.

## 🎯 **Enhanced Logging & Metrics**

### 1. Generic Method Entry Logging

**Objective**: Enhance generic method logging to capture type information and constraints.

**Planned Enhancements**:
- [ ] **Type Parameter Logging**: Log generic type parameters with constraints
- [ ] **Constraint Information**: Display `where T : class`, `where T : IComparable`, etc.
- [ ] **Type Resolution**: Show resolved types for generic method calls
- [ ] **Nested Generic Support**: Handle complex nested generic scenarios

**Example Output**:
```
TRACE: Entering GenericMethod<T> where T : class, IComparable, new()
TRACE: Parameter input = [value] (Type: T)
TRACE: Generic constraint: T must be reference type, comparable, and have default constructor
```

### 2. Method Call Count Metrics

**Objective**: Collect total call count for each intercepted method.

**Planned Features**:
- [ ] **Call Counter**: Increment counter on each method entry
- [ ] **Method Identification**: Unique identifier for each method (class + method name)
- [ ] **Thread Safety**: Atomic counter operations for concurrent access
- [ ] **Reset Capability**: Ability to reset counters for testing/debugging

### 3. Exception Count Metrics

**Objective**: Track total exception count for each method.

**Planned Features**:
- [ ] **Exception Counter**: Increment on each exception
- [ ] **Exception Rate**: Calculate exception rate (exceptions per call)
- [ ] **Exception Types**: Categorize by exception type
- [ ] **Failure Rate**: Percentage of calls that result in exceptions

### 4. Enhanced Exception Detail Logging

**Objective**: Capture comprehensive exception information for debugging.

**Current Status**: ✅ **Basic exception logging implemented**
- ✅ Exception logging in InterceptorAttribute
- ✅ Exception message capture
- ✅ Exception re-throwing

**Potential Additional Enhancements**:
- [ ] **Exception Stack Trace**: Full stack trace capture
- [ ] **Inner Exception Details**: Recursive inner exception logging
- [ ] **Exception Context**: Method parameters at time of exception
- [ ] **Exception Correlation**: Link exceptions to request/trace IDs
- [ ] **Structured Logging**: JSON-formatted exception details

## 🎯 **Performance & Execution Metrics**

### 5. Method Execution Time Metrics

**Objective**: Measure and track method execution performance.

**Planned Features**:
- [ ] **Execution Timer**: Start/stop timing for each method
- [ ] **Performance Statistics**: Min, max, average, percentile execution times
- [ ] **Performance Alerts**: Configurable thresholds for slow methods
- [ ] **Performance Trends**: Track performance over time
- [ ] **Async Method Support**: Handle async method timing correctly

## 🎯 **Advanced Tracing & Observability**

### 6. Configurable Trace ID System

**Current Status**: ✅ **COMPLETED**
- ✅ **TraceContext**: Runtime trace management implemented
- ✅ **Trace ID Registry**: TraceConfig maintains TrackedIds HashSet
- ✅ **Runtime Updates**: ITraceManager provides EnableTracing/DisableTracing methods
- ✅ **Method-Level Control**: InterceptorAttribute checks TraceContext.IsTracingEnabled
- ✅ **Performance Impact**: Minimal overhead when tracing is disabled
- ✅ **HTTP Integration**: Middleware support for extracting trace IDs
- ✅ **Orleans Integration**: Filters for grain-to-grain trace propagation
- ✅ **REST API**: HTTP endpoints for runtime trace management

**Planned Features**:
- [ ] **Trace ID Registry**: Maintain list of active trace IDs
- [ ] **Runtime Updates**: Add/remove trace IDs without restart
- [ ] **Method-Level Control**: Enable tracing for specific methods
- [ ] **Pattern Matching**: Support wildcard patterns for method names
- [ ] **Performance Impact**: Minimal overhead when tracing is disabled
- [ ] **Trace Persistence**: Persist trace configuration across restarts

## 🎯 **Implementation Considerations**

### Performance Impact
- **Metrics Collection**: Target minimal overhead for basic metrics
- **Timing**: Target minimal overhead for execution timing
- **Tracing**: Minimal overhead when disabled (already achieved)

### Memory Management
- **Metrics Storage**: Use concurrent collections for thread safety
- **Data Retention**: Implement configurable data retention policies
- **Memory Leaks**: Ensure proper cleanup of expired metrics

### Scalability
- **High Concurrency**: Support thousands of concurrent method calls
- **Large Applications**: Handle applications with thousands of methods
- **Distributed Systems**: Consider metrics aggregation across instances

## 🎯 **Success Criteria**

### Functional Requirements
- [ ] All 6 planned features implemented and tested
- [ ] Performance overhead within acceptable limits
- [ ] Thread-safe operation under high concurrency
- [ ] Runtime configuration updates working correctly

### Quality Requirements
- [ ] 95%+ test coverage for new features
- [ ] Performance benchmarks documented
- [ ] Comprehensive documentation updated
- [ ] Sample applications demonstrating all features

---

*Last Updated: 2025-08-21*  
*Status: Planning Phase*  
*Next Review: Weekly development meetings*
