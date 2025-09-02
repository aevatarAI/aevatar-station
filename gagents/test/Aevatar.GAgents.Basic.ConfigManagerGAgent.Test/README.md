# ConfigManagerGAgent Performance Tests

This test project contains comprehensive performance tests for the ConfigManagerGAgent Phase 1 optimizations.

## Test Structure

### 📋 Unit Tests
- **ConfigManagerGAgentPerformanceTests.cs**: Core performance unit tests
  - Storage strategy selection tests
  - Compression efficiency tests  
  - Hash computation and change detection
  - Memory usage optimization tests
  - Schema migration tests
  - Event log optimization tests

- **ConfigManagerGAgentIntegrationTests.cs**: Real-world scenario tests
  - Small API configurations (inline storage)
  - Medium feature configurations (compressed storage)
  - Large ML model configurations (external storage)
  - Backward compatibility tests
  - Performance impact analysis

### ⚡ Benchmarks
- **ConfigManagerBenchmarks.cs**: BenchmarkDotNet performance benchmarks
  - Storage strategy selection performance
  - Compression/decompression benchmarks
  - Hash computation benchmarks
  - External storage operation benchmarks

## Running Tests

### Standard Unit Tests
```bash
# Run all unit tests
dotnet test test/Aevatar.GAgents.Basic.Test

# Run specific test class
dotnet test test/Aevatar.GAgents.Basic.Test --filter ConfigManagerGAgentPerformanceTests

# Run with detailed output
dotnet test test/Aevatar.GAgents.Basic.Test --logger "console;verbosity=detailed"
```

### Performance Validation
```bash
# Run basic performance validation
dotnet run --project test/Aevatar.GAgents.Basic.Test

# Run performance validation (Release mode for accurate results)
dotnet run -c Release --project test/Aevatar.GAgents.Basic.Test
```

### Detailed Benchmarks
```bash
# Run BenchmarkDotNet benchmarks
dotnet run -c Release --project test/Aevatar.GAgents.Basic.Test benchmark
```

## Expected Performance Improvements

### Memory Usage Reduction
| Config Size | Strategy   | Memory Reduction |
|-------------|------------|------------------|
| < 1KB       | Inline     | 67% ↓           |
| 1-100KB     | Compressed | 80-90% ↓        |
| > 100KB     | External   | 99%+ ↓          |

### Event Log Size Reduction
- **90%+ reduction** in event log size by storing only hashes instead of full JSON
- **Eliminates redundant** JSON storage across multiple events

### Storage Strategy Performance
- **Inline**: Zero overhead for small configs
- **Compressed**: 70-80% size reduction with minimal CPU overhead
- **External**: Near-zero Orleans state impact for large configs

## Test Coverage

### ✅ Functionality Tests
- [x] Storage strategy selection correctness
- [x] Compression/decompression roundtrip
- [x] Hash-based change detection
- [x] Backward compatibility with legacy schemas
- [x] Event log optimization

### ✅ Performance Tests  
- [x] Memory usage optimization validation
- [x] Compression efficiency analysis
- [x] Storage operation timing
- [x] Real-world configuration scenarios
- [x] Stress testing with various config sizes

### ✅ Integration Tests
- [x] End-to-end configuration management
- [x] Schema migration scenarios
- [x] Multi-strategy configuration handling
- [x] Performance impact measurement

## Benchmark Results Example

```
|                    Method |      Mean |     Error |    StdDev |    Gen 0 |   Allocated |
|-------------------------- |----------:|----------:|----------:|---------:|------------:|
| DetermineStorageStrategy  |  12.34 ns |  0.123 ns |  0.115 ns |        - |           - |
|        CompressConfig_10K | 1,234.5 μs | 12.34 μs  | 11.54 μs | 156.2500 |   1,024 KB |
|      DecompressConfig_10K |   123.4 μs |  1.23 μs  |  1.15 μs |  15.6250 |     102 KB |
|           ComputeHash_10K |    45.6 μs |  0.456 μs |  0.427 μs |   1.2207 |       8 KB |
```

## Test Configuration

### Required Dependencies
- xUnit test framework
- Shouldly assertions
- BenchmarkDotNet for performance benchmarks
- Moq for mocking
- System.IO.Compression for compression tests

### Environment Requirements
- .NET 9.0 or later
- Release mode for accurate performance measurements
- Sufficient memory for large configuration tests

## Interpreting Results

### Unit Test Results
- All tests should pass with green status
- Performance assertions validate optimization claims
- Memory reduction tests confirm space savings

### Benchmark Results
- Look for sub-millisecond strategy selection
- Compression should show 70%+ size reduction
- Hash computation should be under 100μs for typical configs

### Performance Validation
- Memory usage reduction should exceed 30% for all strategies
- Event log optimization should show 80%+ reduction
- Large configs should demonstrate 99%+ memory savings

## Troubleshooting

### Common Issues
1. **Benchmark mode**: Always run benchmarks in Release mode
2. **Memory tests**: Ensure sufficient system memory for large config tests
3. **Timing variations**: Performance may vary based on system load

### Performance Regression Detection
If tests fail or performance degrades:
1. Check for changes in compression algorithms
2. Verify storage strategy thresholds
3. Review hash computation efficiency
4. Validate event log structure changes

## Contributing

When adding new performance tests:
1. Follow existing naming conventions
2. Include both unit tests and integration scenarios  
3. Add benchmark tests for critical paths
4. Update this README with new test descriptions
5. Ensure tests validate actual performance improvements