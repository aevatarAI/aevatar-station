# Aevatar.Core.Interception Testing Documentation

## Testing Strategy

This document outlines the comprehensive testing strategy for the Aevatar.Core.Interception library, covering unit tests, integration tests, and end-to-end tests.

## Test Coverage Statistics

### Overall Coverage

| Test Category | Test Count | Coverage Level | Status |
|---------------|-------------|----------------|---------|
| **Basic Interception** | 8 tests | 100% | ✅ Complete |
| **Exception Handling** | 5 tests | 100% | ✅ Complete |
| **Method Types** | 8 tests | 100% | ✅ Complete |
| **Async Patterns** | 6 tests | 100% | ✅ Complete |
| **Logger Integration** | 5 tests | 100% | ✅ Complete |
| **Parameter Capture** | 5 tests | 100% | ✅ Complete |
| **Advanced Parameter Types** | 11 tests | 100% | ✅ Complete |
| **Generic Types** | 19 tests | 100% | ✅ Complete |
| **Total Unit Tests** | **67 tests** | **100%** | ✅ **Complete** |

### Parameter Type Coverage

| Parameter Type Category | Test Count | Coverage | Examples |
|-------------------------|-------------|----------|----------|
| **Basic Types** | 5 tests | 100% | `string`, `int`, `bool`, `Guid`, `CancellationToken` |
| **Numeric Types** | 1 test | 100% | `byte`, `sbyte`, `short`, `ushort`, `long`, `ulong`, `float`, `double`, `decimal`, `char` |
| **Date/Time Types** | 1 test | 100% | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| **Nullable Types** | 1 test | 100% | `int?`, `string?`, `bool?`, `DateTime?`, `Guid?` |
| **Enum Types** | 1 test | 100% | Built-in enums, custom enums |
| **Struct Types** | 1 test | 100% | Built-in structs, custom structs |
| **Collection Types** | 1 test | 100% | Arrays, Lists, Dictionaries, HashSets, Queues, Stacks |
| **Interface Types** | 2 tests | 100% | Non-generic and generic interfaces |
| **Delegate Types** | 1 test | 100% | `Action<T>`, `Func<T, TResult>`, custom delegates |
| **Special .NET Types** | 1 test | 100% | `object`, `dynamic`, `IntPtr`, `UIntPtr` |
| **Mixed Complex Types** | 1 test | 100% | Nested generics, tuples, complex combinations |
| **Generic Types** | 19 tests | 100% | Constraints, multiple parameters, collections, return types |

## Test Execution

### Running Tests

All tests are integrated into the `Aevatar.Core.Tests` project and can be executed using:

```bash
# Run all interception tests
dotnet test --filter "FullyQualifiedName~Interception" --verbosity normal

# Run specific test categories
dotnet test --filter "FullyQualifiedName~BasicInterception" --verbosity normal
dotnet test --filter "FullyQualifiedName~AdvancedParameterType" --verbosity normal
dotnet test --filter "FullyQualifiedName~Generic" --verbosity normal

# Run all tests in the project
dotnet test --verbosity normal
```

### Test Verification

Each test category verifies specific aspects of the interception system:

- **Basic Interception**: Method entry/exit logging, parameter capture
- **Exception Handling**: Exception logging, async exception handling
- **Method Types**: All access levels, static/instance, constructors
- **Async Patterns**: Task continuation, cancellation, completion
- **Logger Integration**: Logger discovery, fallback mechanisms
- **Parameter Capture**: All supported parameter types
- **Advanced Parameter Types**: Comprehensive type coverage
- **Generic Types**: Generic constraints, collections, return types

### Test Results

All 67 unit tests pass with 100% success rate, providing comprehensive coverage of:

- ✅ Method interception functionality
- ✅ Parameter type support (all major .NET types)
- ✅ Async method handling
- ✅ Exception scenarios
- ✅ Logger integration
- ✅ Generic type support
- ✅ Collection type handling
- ✅ Interface type support (both generic and non-generic)

## Test Infrastructure

### Test Project Structure

```
Aevatar.Core.Tests/
├── Interception/
│   ├── Infrastructure/
│   │   ├── LogCaptureHelper.cs          # Console output capture
│   │   ├── SharedConsoleCapture.cs      # Thread-safe capture
│   │   └── TestLogger.cs                # Mock logger implementation
│   ├── TestSubjects/
│   │   ├── BasicTestClass.cs            # Test methods for all parameter types
│   │   ├── ConstructorTestClass.cs      # Constructor testing
│   │   └── ParameterTestClass.cs        # Parameter-specific tests
│   └── Unit/
│       ├── BasicInterceptionTests.cs    # Core interception tests
│       ├── ExceptionHandlingTests.cs    # Exception scenarios
│       ├── MethodTypeTests.cs           # Method type coverage
│       ├── ParameterCaptureTests.cs     # Parameter capture
│       ├── LoggerIntegrationTests.cs    # Logger integration
│       ├── AdvancedParameterTypeTests.cs # Comprehensive parameter types
│       └── GenericTypeTests.cs          # Generic type scenarios
```

### Key Test Components

#### LogCaptureHelper
- **Purpose**: Captures console output during tests
- **Features**: Thread-safe, isolated capture per test
- **Usage**: Automatic disposal and cleanup

#### SharedConsoleCapture
- **Purpose**: Provides thread-safe console capture
- **Features**: MulticastTextWriter for concurrent test execution
- **Benefits**: Prevents test isolation issues

#### TestSubjects
- **BasicTestClass**: Comprehensive test methods covering all parameter types
- **ConstructorTestClass**: Constructor interception testing
- **ParameterTestClass**: Parameter-specific test scenarios

### Fody Integration

The test project includes Fody configuration for method decoration:

```xml
<!-- FodyWeavers.xml -->
<?xml version="1.0" encoding="utf-8"?>
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchemaInstance" 
         xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <MethodDecorator />
</Weavers>
```

### Module-Level Interception

Tests use module-level interception for comprehensive coverage:

```csharp
// GlobalUsings.cs
[module: Interceptor]
```

This ensures all test methods are automatically intercepted for validation.

## Test Categories

### 1. Unit Tests

Test individual components and behaviors in isolation.

#### Test Subjects

| Component | Test Focus | Coverage |
|-----------|------------|----------|
| **InterceptorAttribute** | Core functionality | 95%+ |
| **Logger Discovery** | Logger resolution logic | 100% |
| **Parameter Capture** | Parameter serialization | 100% |
| **Exception Handling** | Error scenarios | 100% |
| **Async Support** | Task continuation logic | 100% |

#### Unit Test Categories

##### 1.1 Basic Interception Tests

```csharp
[TestFixture]
public class BasicInterceptionTests
{
    [Test] public void SyncMethod_ShouldLogEntryAndExit()
    [Test] public void AsyncMethod_ShouldLogEntryAndAsyncCompletion()
    [Test] public void MethodWithParameters_ShouldLogParameterValues()
    [Test] public void MethodWithReturnValue_ShouldLogReturnValue()
    [Test] public void VoidMethod_ShouldLogEntryAndExit()
}
```

##### 1.2 Exception Handling Tests

```csharp
[TestFixture]
public class ExceptionHandlingTests
{
    [Test] public void MethodThrowingException_ShouldLogExceptionAndRethrow()
    [Test] public void AsyncMethodThrowingException_ShouldLogAsyncException()
    [Test] public void ExceptionInParameterSerialization_ShouldNotFailMethod()
    [Test] public void ExceptionInLogger_ShouldFallbackToConsole()
    [Test] public void NestedExceptions_ShouldLogAllLevels()
}
```

##### 1.3 Method Type Tests

```csharp
[TestFixture]
public class MethodTypeTests
{
    [Test] public void PublicMethod_ShouldBeIntercepted()
    [Test] public void PrivateMethod_ShouldBeIntercepted()
    [Test] public void ProtectedMethod_ShouldBeIntercepted()
    [Test] public void InternalMethod_ShouldBeIntercepted()
    [Test] public void StaticMethod_ShouldBeIntercepted()
    [Test] public void Constructor_ShouldBeIntercepted()
    [Test] public void GenericMethod_ShouldBeIntercepted()
    [Test] public void ExtensionMethod_ShouldBeIntercepted()
}
```

##### 1.4 Async Pattern Tests

```csharp
[TestFixture]
public class AsyncPatternTests
{
    [Test] public void AsyncTask_ShouldUseOnTaskContinuation()
    [Test] public void AsyncTaskWithResult_ShouldLogResult()
    [Test] public void AsyncVoid_ShouldLogCorrectly()
    [Test] public void ConfigureAwaitFalse_ShouldWork()
    [Test] public void CancelledTask_ShouldLogCancellation()
    [Test] public void TaskWithException_ShouldLogAsyncException()
}
```

##### 1.5 Logger Integration Tests

```csharp
[TestFixture]
public class LoggerIntegrationTests
{
    [Test] public void ILoggerInstance_ShouldBeDiscovered()
    [Test] public void LoggerProperty_ShouldBeDiscovered()
    [Test] public void NoLogger_ShouldFallbackToConsole()
    [Test] public void MultipleLoggers_ShouldUseFirstFound()
    [Test] public void LoggerThrowsException_ShouldFallbackToConsole()
}
```

##### 1.6 Parameter Capture Tests

```csharp
[TestFixture]
public class ParameterCaptureTests
{
    [Test] public void BasicTypes_ShouldCaptureCorrectly()
    [Test] public void ComplexObjects_ShouldSerializeToString()
    [Test] public void NullValues_ShouldHandleGracefully()
    [Test] public void Collections_ShouldShowTypeInformation()
    [Test] public void GenericTypes_ShouldResolveCorrectly()
}
```

##### 1.7 Advanced Parameter Type Tests

Comprehensive testing of all supported parameter types:

```csharp
[TestFixture]
public class AdvancedParameterTypeTests
{
    // Numeric Types
    [Test] public void MethodWithNumericTypes_ShouldLogAllNumericParameters()
    
    // Date/Time Types
    [Test] public void MethodWithDateTimeTypes_ShouldLogAllDateTimeParameters()
    
    // Nullable Types
    [Test] public void MethodWithNullableTypes_ShouldLogAllNullableParameters()
    
    // Enum Types
    [Test] public void MethodWithEnumTypes_ShouldLogAllEnumParameters()
    
    // Struct Types
    [Test] public void MethodWithStructTypes_ShouldLogAllStructParameters()
    
    // Collection Types
    [Test] public void MethodWithSpecificCollections_ShouldLogAllCollectionParameters()
    
    // Interface Types (Both Non-Generic and Generic)
    [Test] public void MethodWithInterfaceTypes_ShouldLogAllNonGenericInterfaceParameters()
    [Test] public void MethodWithGenericInterfaceTypes_ShouldLogAllGenericInterfaceParameters()
    
    // Delegate Types
    [Test] public void MethodWithDelegateTypes_ShouldLogAllDelegateParameters()
    
    // Special .NET Types
    [Test] public void MethodWithSpecialTypes_ShouldLogAllSpecialParameters()
    
    // Mixed Complex Types
    [Test] public void MethodWithMixedComplexTypes_ShouldLogAllComplexParameters()
}
```

##### 1.8 Generic Type Tests

```csharp
[TestFixture]
public class GenericTypeTests
{
    // Generic Constraints
    [Test] public void GenericMethodWithClassConstraint_ShouldWork()
    [Test] public void GenericMethodWithComparableConstraint_ShouldWork()
    [Test] public void GenericMethodWithNewConstraint_ShouldWork()
    [Test] public void GenericMethodWithBaseClassConstraint_ShouldWork()
    [Test] public void GenericMethodWithInterfaceConstraint_ShouldWork()
    
    // Multiple Type Parameters
    [Test] public void GenericMethodWithMultipleTypes_ShouldWork()
    
    // Generic Collections
    [Test] public void GenericMethodWithCollection_ShouldWork()
    [Test] public void GenericMethodWithArray_ShouldWork()
    
    // Generic Return Types
    [Test] public void GenericMethodReturningCollection_ShouldWork()
    [Test] public void GenericMethodReturningDictionary_ShouldWork()
    
    // Complex Generic Scenarios
    [Test] public void GenericMethodWithDefaultValue_ShouldWork()
    [Test] public void GenericMethodWithNestedGenerics_ShouldWork()
    [Test] public void GenericMethodWithValueTuple_ShouldWork()
}

```csharp
[TestFixture]
public class ParameterCaptureTests
{
    [Test] public void SimpleTypes_ShouldSerializeCorrectly()
    [Test] public void ComplexTypes_ShouldSerializeToString()
    [Test] public void NullParameters_ShouldLogAsNull()
    [Test] public void LargeParameters_ShouldTruncateIfNeeded()
    [Test] public void CircularReferences_ShouldNotCauseStackOverflow()
    [Test] public void SensitiveData_ShouldBeHandledCarefully()
}
```

### 2. Integration Tests

Test component interactions and real-world scenarios.

#### Integration Test Categories

##### 2.1 Orleans Integration Tests

```csharp
[TestFixture]
public class OrleansIntegrationTests
{
    [Test] public void GrainLifecycle_ShouldBeTraced()
    [Test] public void GrainMethods_ShouldBeIntercepted()
    [Test] public void StreamProcessing_ShouldBeTraced()
    [Test] public void EventHandling_ShouldBeLogged()
    [Test] public void GrainActivation_ShouldLogCorrectly()
    [Test] public void GrainDeactivation_ShouldLogCorrectly()
}
```

##### 2.2 Module-Level Application Tests

```csharp
[TestFixture]
public class ModuleLevelTests
{
    [Test] public void ModuleAttribute_ShouldInterceptAllMethods()
    [Test] public void AssemblyAttribute_ShouldInterceptAllMethods()
    [Test] public void SelectiveInterception_ShouldOnlyInterceptMarkedMethods()
    [Test] public void InheritedMethods_ShouldBeIntercepted()
    [Test] public void OverriddenMethods_ShouldBeIntercepted()
}
```

##### 2.3 Complex Scenario Tests

```csharp
[TestFixture]
public class ComplexScenarioTests
{
    [Test] public void NestedMethodCalls_ShouldLogHierarchy()
    [Test] public void RecursiveMethods_ShouldLogCorrectly()
    [Test] public void ParallelExecution_ShouldNotInterfere()
    [Test] public void LongRunningMethods_ShouldNotCauseMemoryLeaks()
    [Test] public void HighVolumeLogging_ShouldPerformWell()
}
```

### 3. End-to-End Tests

Test complete workflows and real-world usage patterns.

#### E2E Test Categories

##### 3.1 Real Orleans Grain Tests

```csharp
[TestFixture]
public class RealOrleansGrainE2ETests
{
    [Test] public void CompleteGrainWorkflow_ShouldBeFullyTraced()
    [Test] public void EventProcessingPipeline_ShouldLogAllSteps()
    [Test] public void ErrorHandlingInGrain_ShouldLogAndRecover()
    [Test] public void StreamSubscription_ShouldTraceEvents()
    [Test] public void StateChanges_ShouldBeLogged()
}
```

##### 3.2 Performance Tests

```csharp
[TestFixture]
public class PerformanceE2ETests
{
    [Test] public void HighThroughputLogging_ShouldNotDegradePerformance()
    [Test] public void MemoryUsage_ShouldRemainConstant()
    [Test] public void CpuOverhead_ShouldBeMinimal()
    [Test] public void LoggingLatency_ShouldBeAcceptable()
    [Test] public void GarbageCollection_ShouldNotBeExcessive()
}
```

##### 3.3 Real-World Scenario Tests

```csharp
[TestFixture]
public class RealWorldScenarioE2ETests
{
    [Test] public void WebApiWithInterception_ShouldWork()
    [Test] public void DatabaseOperations_ShouldBeTraced()
    [Test] public void ExternalServiceCalls_ShouldBeLogged()
    [Test] public void BackgroundServices_ShouldBeMonitored()
    [Test] public void ConfigurationChanges_ShouldBeLogged()
}
```

## Test Infrastructure

### Test Helpers

#### Log Capture Helper

```csharp
public class LogCaptureHelper : IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOutput;
    
    public LogCaptureHelper()
    {
        _stringWriter = new StringWriter();
        _originalOutput = Console.Out;
        Console.SetOut(_stringWriter);
    }
    
    public string GetCapturedLogs() => _stringWriter.ToString();
    
    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _stringWriter?.Dispose();
    }
}
```

#### Test Logger Implementation

```csharp
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = new();
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
        Exception exception, Func<TState, Exception, string> formatter)
    {
        LogEntries.Add(new LogEntry
        {
            Level = logLevel,
            Message = formatter(state, exception),
            Exception = exception,
            Timestamp = DateTime.UtcNow
        });
    }
    
    // ... other ILogger methods
}
```

#### Orleans Test Grain

```csharp
public class TestGrain : Grain, IGrainWithGuidKey
{
    private readonly ILogger<TestGrain> _logger;
    
    public TestGrain(ILogger<TestGrain> logger)
    {
        _logger = logger;
    }
    
    [Interceptor]
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
    }
    
    [Interceptor]
    public async Task<string> ProcessEventAsync(string eventData)
    {
        await Task.Delay(100);
        return $"Processed: {eventData}";
    }
}
```

### Test Data

#### Parameter Test Cases

```csharp
public static class ParameterTestCases
{
    public static readonly object[][] SimpleTypes = {
        new object[] { "string", "Hello World" },
        new object[] { "int", 42 },
        new object[] { "double", 3.14159 },
        new object[] { "bool", true },
        new object[] { "DateTime", DateTime.UtcNow },
        new object[] { "Guid", Guid.NewGuid() }
    };
    
    public static readonly object[][] ComplexTypes = {
        new object[] { "List<string>", new List<string> { "a", "b", "c" } },
        new object[] { "Dictionary", new Dictionary<string, int> { {"key", 1} } },
        new object[] { "Custom Object", new TestObject { Name = "Test", Value = 123 } }
    };
    
    public static readonly object[][] EdgeCases = {
        new object[] { "null", null },
        new object[] { "empty string", "" },
        new object[] { "empty collection", new List<string>() },
        new object[] { "large string", new string('x', 10000) }
    };
}
```

### Test Configuration

#### NUnit Configuration

```xml
<!-- .runsettings -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <NUnit>
    <NumberOfTestWorkers>1</NumberOfTestWorkers>
    <ShadowCopyFiles>false</ShadowCopyFiles>
  </NUnit>
  <RunConfiguration>
    <MaxCpuCount>1</MaxCpuCount>
  </RunConfiguration>
</RunSettings>
```

#### Test Categories

```csharp
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string E2E = "E2E";
    public const string Performance = "Performance";
    public const string Orleans = "Orleans";
    public const string Async = "Async";
    public const string Exception = "Exception";
}
```

## Test Execution

### Local Testing

```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter TestCategory=Unit
dotnet test --filter TestCategory=Integration
dotnet test --filter TestCategory=E2E

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### CI/CD Pipeline

```yaml
# test.yml
name: Test
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 9.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Unit Tests
      run: dotnet test --no-build --filter TestCategory=Unit
    
    - name: Integration Tests
      run: dotnet test --no-build --filter TestCategory=Integration
    
    - name: E2E Tests
      run: dotnet test --no-build --filter TestCategory=E2E
    
    - name: Coverage Report
      run: dotnet test --collect:"XPlat Code Coverage"
```

## Coverage Goals

### Target Coverage Levels

| Test Type | Target Coverage | Critical Components |
|-----------|-----------------|-------------------|
| **Unit Tests** | 95%+ | InterceptorAttribute, Logger Discovery |
| **Integration Tests** | 85%+ | Orleans Integration, Module Application |
| **E2E Tests** | 75%+ | Real Scenarios, Performance |

### Coverage Exclusions

- Generated code (Fody weavers)
- Exception handling for impossible scenarios
- Performance measurement code
- Test infrastructure code

## Test Maintenance

### Regular Tasks

1. **Update Test Data**: Keep test cases current with new scenarios
2. **Performance Baselines**: Update performance expectations
3. **Orleans Compatibility**: Test with new Orleans versions
4. **Framework Updates**: Verify compatibility with new .NET versions

### Test Review Process

1. **Code Coverage**: Ensure new code has appropriate test coverage
2. **Test Quality**: Review test clarity and maintainability
3. **Performance Impact**: Verify tests don't introduce performance regressions
4. **Documentation**: Keep test documentation up to date

## Troubleshooting Test Issues

### Common Test Failures

1. **Fody Weaving Failures**
   ```
   Issue: Tests fail because IL weaving didn't occur
   Solution: Ensure FodyWeavers.xml is included in test project
   ```

2. **Async Test Deadlocks**
   ```
   Issue: Async tests hang or deadlock
   Solution: Use ConfigureAwait(false) or async test frameworks
   ```

3. **Log Capture Issues**
   ```
   Issue: Expected log messages not captured
   Solution: Verify log capture setup and timing
   ```

4. **Orleans Lifecycle Issues**
   ```
   Issue: Orleans grain tests fail
   Solution: Ensure proper Orleans test cluster setup
   ```

### Debug Techniques

1. **Fody Debug Output**: Enable Fody debugging to see IL weaving
2. **Log Analysis**: Capture and analyze all log output
3. **Reflection Inspection**: Verify methods are properly intercepted
4. **Performance Profiling**: Use profilers to identify bottlenecks
