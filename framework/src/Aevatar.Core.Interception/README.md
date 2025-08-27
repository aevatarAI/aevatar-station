# Aevatar.Core.Interception

A comprehensive method interception library for the Aevatar framework, providing automatic tracing, logging, and monitoring capabilities using Fody MethodDecorator.

## Features

🚀 **Zero Runtime Overhead** - Compile-time IL weaving  
🔍 **Complete Coverage** - All method types and access levels  
⚡ **Async Support** - Full async/await pattern support  
🏗️ **Orleans Integration** - Seamless Orleans grain lifecycle tracking  
📊 **Parameter Capture** - Automatic parameter logging  
🛡️ **Exception Safe** - Robust exception handling  
📝 **Flexible Logging** - Multiple logging backends  

## Quick Start

### 1. Installation

Add the package reference to your project:

```xml
<PackageReference Include="MethodDecorator.Fody" Version="1.1.1" />
```

### 2. Configuration

Create `FodyWeavers.xml` in your project root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
         xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <MethodDecorator />
</Weavers>
```

### 3. Basic Usage

```csharp
using Aevatar.Core.Interception;

// Module-level interception (all methods)
[module: Interceptor]

public class MyService
{
    public void DoWork()
    {
        // Automatically traced:
        // TRACE: Entering DoWork
        // TRACE: Exiting DoWork
    }
    
    [Interceptor]
    public async Task ProcessAsync(string input, int count)
    {
        // Automatically traced:
        // TRACE: Entering ProcessAsync
        // TRACE: Parameter input = Hello
        // TRACE: Parameter count = 42
        // ... method execution ...
        // TRACE: Async completed ProcessAsync
    }
}
```

## Usage Patterns

### Method-Level Interception

Apply to specific methods:

```csharp
public class MyService
{
    [Interceptor]
    public void CriticalMethod()
    {
        // Only this method is traced
    }
    
    public void RegularMethod()
    {
        // Not traced
    }
}
```

### Orleans Grain Integration

```csharp
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

### Exception Handling

```csharp
public class MyService
{
    [Interceptor]
    public void RiskyMethod()
    {
        throw new InvalidOperationException("Something went wrong");
        // Automatically logged:
        // TRACE: Exception in RiskyMethod: InvalidOperationException: Something went wrong
    }
}
```

### Private Method Tracing

```csharp
public class MyService
{
    public void PublicMethod()
    {
        HelperMethod(); // Also traced!
    }
    
    [Interceptor]
    private void HelperMethod()
    {
        // Private methods are also intercepted
        // TRACE: Entering HelperMethod
        // TRACE: Exiting HelperMethod
    }
}
```

## Supported Method Types

| Method Type | Support | Example |
|-------------|---------|---------|
| **Public Methods** | ✅ | `public void DoWork()` |
| **Private Methods** | ✅ | `private void Helper()` |
| **Protected Methods** | ✅ | `protected virtual void Process()` |
| **Internal Methods** | ✅ | `internal void Utility()` |
| **Static Methods** | ✅ | `public static void Factory()` |
| **Constructors** | ✅ | `public MyClass(string name)` |
| **Async Methods** | ✅ | `public async Task<T> GetAsync()` |
| **Generic Methods** | ✅ | `public T Process<T>(T input)` |
| **Extension Methods** | ✅ | `public static void Extend(this T obj)` |

## Comprehensive Parameter Type Support

The interceptor provides extensive parameter type coverage including primitives, collections, generics, nullable types, enums, delegates, and advanced .NET types. For complete details on supported types and parameter capture strategy, see [Design Document](docs/DESIGN.md).

## Logging Integration

### ILogger Support

The interceptor automatically discovers loggers:

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger; // Automatically discovered
    }
    
    [Interceptor]
    public void DoWork()
    {
        // Uses _logger for output instead of console
    }
}
```

### Logger Property Discovery

```csharp
public class MyService
{
    public ILogger Logger { get; set; } // Automatically discovered
    
    [Interceptor]
    public void DoWork()
    {
        // Uses Logger property for output
    }
}
```

### Console Fallback

When no logger is found, output goes to console:

```csharp
public class MyService
{
    [Interceptor]
    public void DoWork()
    {
        // Output: Console.WriteLine("TRACE: Entering DoWork")
    }
}
```

## Configuration Options

### Attribute Targets

```csharp
[AttributeUsage(AttributeTargets.Method | 
                AttributeTargets.Constructor | 
                AttributeTargets.Assembly | 
                AttributeTargets.Module)]
public class InterceptorAttribute : Attribute
```

### Application Levels

```csharp
// Assembly level - all methods in assembly
[assembly: Interceptor]

// Module level - all methods in module
[module: Interceptor]

// Class level - all methods in class (not supported by MethodDecorator)
// [Interceptor] // Not valid on classes

// Method level - specific method
[Interceptor]
public void SpecificMethod() { }
```

## Performance Considerations

### Compile-Time Weaving

- **Zero Runtime Overhead**: IL weaving happens at build time
- **No Reflection**: Direct method calls, no runtime reflection
- **Minimal IL Expansion**: Efficient instruction injection

### Runtime Performance

- **Fast Parameter Capture**: Only when parameters exist
- **Logger Caching**: Logger discovery cached per instance
- **Exception Handling**: Minimal overhead for try-catch blocks

### Memory Impact

- **Small Footprint**: 4 reference fields per intercepted method
- **On-Demand Allocation**: Strings allocated only when logging
- **No Memory Leaks**: Proper reference management

## Error Handling

### Exception Safety

The interceptor never interferes with your original method behavior:

```csharp
[Interceptor]
public void RiskyMethod()
{
    throw new Exception("Original exception");
    // Exception is logged and then re-thrown unchanged
}
```

### Failure Modes

- **Logger Not Found**: Falls back to Console.WriteLine
- **Parameter Serialization Fails**: Logs "serialization failed"
- **Interceptor Exception**: Logs error and continues execution

## Testing

### Unit Testing

Test intercepted methods normally:

```csharp
[Test]
public void TestInterceptedMethod()
{
    var service = new MyService();
    service.DoWork(); // Works exactly the same, just with tracing
}
```

### Verifying Interception

Use log capture to verify tracing:

```csharp
[Test]
public void TestInterceptionLogging()
{
    var logOutput = new StringWriter();
    Console.SetOut(logOutput);
    
    var service = new MyService();
    service.DoWork();
    
    var output = logOutput.ToString();
    Assert.Contains("TRACE: Entering DoWork", output);
    Assert.Contains("TRACE: Exiting DoWork", output);
}
```

### Parameter Type Testing

The interceptor includes comprehensive tests for all supported parameter types:

```csharp
[Test]
public void TestNumericParameterTypes()
{
    var service = new MyService();
    service.MethodWithNumericTypes(255, -128, -32768, 65535, 
        -9223372036854775808L, 18446744073709551615UL, 
        3.14159f, 2.718281828459045, 123.456789m, 'A');
    
    // Verifies interception of all numeric types
    // TRACE: Parameter byteValue = 255
    // TRACE: Parameter charValue = A
    // etc.
}

[Test]
public void TestCollectionParameterTypes()
{
    var service = new MyService();
    service.MethodWithCollections(
        new byte[] { 1, 2, 3 },
        new List<string> { "item1", "item2" },
        new Dictionary<int, string> { { 1, "one" } }
    );
    
    // Verifies interception of arrays, lists, dictionaries
    // TRACE: Parameter byteArray = System.Byte[]
    // TRACE: Parameter stringList = System.Collections.Generic.List`1[System.String]
    // etc.
}
```

## Runtime Trace Control

The library provides runtime control over tracing through a configurable trace ID system, allowing you to enable or disable tracing for specific requests or operations without restarting the application.

```csharp
// Enable tracing for a specific trace ID
TraceContext.EnableTracing("user-session-123");

// Check if tracing is enabled
if (TraceContext.IsTracingEnabled)
{
    // Method will be intercepted and logged
    MyInterceptedMethod();
}
```

For complete documentation including HTTP integration, Orleans support, and REST API endpoints, see [Runtime Trace Control](docs/RUNTIME_TRACE_CONTROL.md).

## Advanced Scenarios

### Complex Call Chains

```csharp
public class ComplexService
{
    [Interceptor]
    public async Task ProcessAsync()
    {
        // TRACE: Entering ProcessAsync
        await Step1Async();
        await Step2Async();
        // TRACE: Async completed ProcessAsync
    }
    
    [Interceptor]
    private async Task Step1Async()
    {
        // TRACE: Entering Step1Async
        // ... work ...
        // TRACE: Async completed Step1Async
    }
    
    [Interceptor]
    private async Task Step2Async()
    {
        // TRACE: Entering Step2Async
        // ... work ...
        // TRACE: Async completed Step2Async
    }
}
```

### Generic Methods

```csharp
public class GenericService
{
    [Interceptor]
    public async Task<T> ProcessAsync<T>(T input)
    {
        // TRACE: Entering ProcessAsync
        // TRACE: Parameter input = [value]
        // ... processing ...
        // TRACE: Async completed ProcessAsync
        return input;
    }
}
```

### Extension Methods

```csharp
public static class Extensions
{
    [Interceptor]
    public static async Task<string> ProcessAsync(this string input, int count)
    {
        // TRACE: Entering ProcessAsync
        // TRACE: Parameter input = Hello
        // TRACE: Parameter count = 3
        // ... processing ...
        // TRACE: Async completed ProcessAsync
        return result;
    }
}
```

## Troubleshooting

### Common Issues

1. **FodyWeavers.xml Missing**
   ```
   Error: Could not find a FodyWeavers.xml file
   Solution: Create FodyWeavers.xml in project root
   ```

2. **No Tracing Output**
   ```
   Check: Is [Interceptor] attribute applied?
   Check: Is MethodDecorator in FodyWeavers.xml?
   Check: Did project rebuild after adding attribute?
   ```

3. **Build Errors**
   ```
   Error: Attribute 'Interceptor' is not valid on this declaration type
   Solution: Only use on methods, constructors, assembly, or module
   ```

### Debugging

Enable Fody verbose logging:

```xml
<Project>
  <PropertyGroup>
    <FodyDebug>true</FodyDebug>
  </PropertyGroup>
</Project>
```

## Examples

See the `samples/InterceptorDemo` project for comprehensive examples covering:

- Basic method interception
- Orleans grain integration
- Async method handling
- Exception scenarios
- Non-public method tracing
- Generic method support
- Extension method tracing

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is part of the Aevatar framework and follows the same license terms.

## Related Projects

- [Fody](https://github.com/Fody/Fody) - IL weaving framework
- [MethodDecorator.Fody](https://github.com/Fody/MethodDecorator) - Method decoration support
- [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging) - Logging abstractions

## Documentation

- [Design Document](docs/DESIGN.md) - Architecture and design details
- [Testing Documentation](docs/TESTING.md) - Testing strategy and coverage
- [Runtime Trace Control](docs/RUNTIME_TRACE_CONTROL.md) - Runtime trace ID management
- [TODO](docs/TODO.md) - Planned enhancements and roadmap
