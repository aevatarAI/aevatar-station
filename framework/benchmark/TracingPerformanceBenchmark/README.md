# Tracing Performance Benchmark

This benchmark project evaluates the performance differences between two tracing approaches in the Aevatar Framework:

1. **No Tracing** - Baseline performance without any tracing overhead
2. **Fody IL Weaving** - Build-time code injection using Fody weavers

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code

### Build and Run
```bash
# Navigate to framework benchmark directory
cd aevatar-station/framework/benchmark/TracingPerformanceBenchmark

# Build the project
dotnet build

# Run the benchmark
dotnet run -c Release

### Environment-Specific Configuration
```bash
# Fody mode
dotnet run --environment Fody

# No Tracing mode
dotnet run --environment NoTracing
```
```

## 📊 Benchmark Categories

### 1. **No Tracing (Baseline)**
- **Purpose**: Establish baseline performance without any tracing overhead
- **Implementation**: Direct service calls with no interception
- **Expected Performance**: Fastest execution time, lowest memory allocation

### 2. **Fody IL Weaving**
- **Purpose**: Build-time code injection for tracing
- **Implementation**: Fody weavers modify IL at build time
- **Expected Performance**: Minimal runtime overhead, code is pre-injected
- **Use Case**: Production deployments with stable tracing configuration

## 🔧 Configuration

### Build Configuration
The benchmark supports different build configurations:

```bash
# Build with no tracing (baseline)
dotnet build -c NoTracing

# Build with Fody weaving
dotnet build -c Fody
```

### Fody Configuration
```xml
<!-- FodyWeavers.xml -->
<Weavers>
  <MethodTracing>
    <ActivitySourceName>Aevatar.MethodTracing</ActivitySourceName>
    <EnableParameterCapture>true</EnableParameterCapture>
    <EnableReturnValueCapture>true</EnableReturnValueCapture>
    <MaxCaptureSize>1000</MaxCaptureSize>
  </MethodTracing>
</Weavers>
```

## 📈 Benchmark Metrics

### Performance Metrics
- **Execution Time**: Method call duration in nanoseconds
- **Memory Allocation**: Bytes allocated per operation
- **CPU Usage**: Processor time per operation
- **Throughput**: Operations per second

### Benchmark Categories
- **NoTracing**: Baseline performance measurements
- **Fody**: Fody weaving performance measurements
- **Memory**: Memory allocation comparisons

## 🏗️ Architecture

### Service Layer
```csharp
public interface IOrderService
{
    Task<OrderResult> CreateOrderAsync(OrderRequest request);
    Task<OrderResult> ProcessOrderAsync(string orderId);
    Task<OrderResult> GetOrderAsync(string orderId);
}
```

### Tracing Implementations
1. **OrderService**: No tracing, direct implementation
2. **FodyOrderService**: Fody-processed with [Trace] attributes

### Benchmark Structure
```csharp
[Benchmark(Baseline = true)]
public async Task<OrderResult> NoTracing_CreateOrder()
{
    return await _noTracingOrderService.CreateOrderAsync(_testRequest);
}

[Benchmark]
public async Task<OrderResult> Fody_CreateOrder()
{
    TraceContext.ActiveTraceId = _testTraceId;
    try
    {
        return await _fodyOrderService.CreateOrderAsync(_testRequest);
    }
    finally
    {
        TraceContext.Clear();
    }
}
```

## 📊 Expected Results

### Performance Characteristics

| Approach | Execution Time | Memory Overhead | Runtime Flexibility | Build Time |
|----------|----------------|-----------------|-------------------|------------|
| **No Tracing** | ⚡ Fastest | 🟢 Minimal | ❌ None | ⚡ Fast |
| **Fody** | 🟢 Fast | 🟢 Low | 🟡 Low | 🟡 Moderate |

### Performance Metrics
The benchmark will measure:
- Memory allocation per operation
- Execution time compared to baseline
- Actual performance overhead of each approach

## 🔍 Analysis

### When to Use Each Approach

#### **No Tracing**
- **Use Case**: Production environments where tracing is not needed
- **Benefits**: Maximum performance, minimal memory usage
- **Drawbacks**: No observability, no debugging capabilities

#### **Fody IL Weaving**
- **Use Case**: Production environments with stable tracing needs
- **Benefits**: Minimal runtime overhead, pre-optimized code
- **Drawbacks**: Build-time processing, less runtime flexibility

## 🚀 Running Custom Benchmarks

### Adding New Benchmarks
```csharp
[Benchmark]
[BenchmarkCategory("Custom")]
public async Task<OrderResult> CustomBenchmark()
{
    // Your custom benchmark logic here
    return await _service.CustomOperationAsync();
}
```

### Custom Categories
```csharp
[BenchmarkCategory("Custom", "Performance")]
public async Task<OrderResult> CustomPerformanceBenchmark()
{
    // Performance-focused benchmark
}
```

## 📋 Benchmark Results

After running the benchmark, you'll see detailed results including:

- **Mean execution time** for each approach
- **Memory allocation** per operation
- **Standard deviation** and confidence intervals
- **Performance ratios** compared to baseline
- **Memory overhead** analysis

## 🔧 Troubleshooting

### Common Issues

#### **Build Errors**
- Ensure all dependencies are restored: `dotnet restore`
- Check .NET version compatibility
- Verify Fody weavers are properly configured

#### **Runtime Errors**
- Check service registration in benchmark setup
- Ensure TraceContext is properly initialized

#### **Performance Issues**
- Run in Release mode: `dotnet run -c Release`
- Ensure no other processes are consuming resources
- Run multiple iterations for statistical significance

## 📚 References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Fody Documentation](https://github.com/Fody/Fody)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)

## 🤝 Contributing

To add new benchmarks or improve existing ones:

1. Create a new benchmark method with appropriate attributes
2. Add new service implementations if needed
3. Update documentation and README
4. Test with different configurations
5. Submit pull request with detailed description

## 📄 License

This benchmark project is part of the Aevatar Framework and follows the same licensing terms.