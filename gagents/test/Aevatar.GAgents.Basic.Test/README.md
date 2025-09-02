# Aevatar.GAgents.Basic.Test

This test project contains comprehensive unit tests for the Basic GAgents, with a special focus on the ConfigManagerGAgent Phase 1 performance optimizations.

## 🚀 Overview

The test suite validates the ConfigManagerGAgent's new intelligent storage strategies and performance improvements, ensuring optimal handling of configurations of all sizes.

## 📋 Test Structure

### 🧪 Core Test Files

#### `ConfigManagerGAgentTests.cs` - Main Unit Tests
Comprehensive test coverage for ConfigManagerGAgent including:

- **Storage Strategy Tests**
  - Small config (< 1KB) → Inline storage
  - Medium config (1KB-100KB) → Compressed storage  
  - Large config (> 100KB) → External storage

- **Configuration Retrieval Tests**
  - Inline configuration retrieval
  - Compressed configuration decompression
  - External configuration retrieval

- **Hash and Change Detection Tests**
  - Consistent hash calculation
  - Configuration change detection
  - Hash-based optimization validation

- **Error Handling Tests**
  - Invalid JSON handling
  - Empty configuration type validation
  - Non-existent configuration requests

- **Backward Compatibility Tests**
  - Legacy state structure migration
  - Legacy configuration handling

- **Performance Tests**
  - Multiple configuration updates
  - Compression efficiency validation
  - Memory usage optimization

#### `ConfigStorageServiceTests.cs` - Storage Service Tests
Focused tests for the IConfigStorageService implementation:

- **External Storage Operations**
  - Store and retrieve operations
  - Configuration deletion
  - Non-existent key handling
  - Large configuration support

- **Compression Utility Tests**
  - String compression/decompression
  - JSON structure preservation
  - High compression ratio validation
  - Empty string handling

#### `ConfigManagerPerformanceBenchmarks.cs` - Performance Benchmarks
BenchmarkDotNet-powered performance tests:

- **Storage Strategy Selection Benchmarks**
- **Compression/Decompression Benchmarks**
- **External Storage Operation Benchmarks**
- **Hash Calculation Benchmarks**
- **JSON Parsing Benchmarks**

## 🏃‍♂️ Running Tests

### Standard Unit Tests
```bash
# Run all tests
dotnet test test/Aevatar.GAgents.Basic.Test/

# Run with verbose output
dotnet test test/Aevatar.GAgents.Basic.Test/ --logger "console;verbosity=normal"

# Run specific test class
dotnet test test/Aevatar.GAgents.Basic.Test/ --filter "ConfigManagerGAgentTests"
```

### Performance Benchmarks
```bash
# Run basic performance validation
dotnet run --project test/Aevatar.GAgents.Basic.Test --configuration Release

# Run full BenchmarkDotNet benchmarks
dotnet run --project test/Aevatar.GAgents.Basic.Test --configuration Release benchmark
```

## 📊 Expected Performance Improvements

### Storage Optimization Results

| Configuration Size | Storage Strategy | Memory Reduction | Event Log Reduction | Compression Ratio |
|-------------------|------------------|------------------|-------------------|------------------|
| < 1KB             | Inline           | 67% ↓           | 90% ↓             | N/A              |
| 1KB-100KB         | Compressed       | 80-90% ↓        | 90% ↓             | 70%+ ↓          |
| > 100KB           | External         | 99%+ ↓          | 95% ↓             | N/A              |

### Performance Metrics

- **Configuration Update Speed**: 3-5x faster for large configurations
- **Memory Usage**: 30-99% reduction depending on configuration size
- **Orleans State Size**: Dramatically reduced for all configuration types
- **Event Log Efficiency**: 80-95% reduction in event storage size

## 🔧 Configuration

Tests use the following configuration categories:

### Small Configurations (< 1KB)
- API endpoint configurations
- Feature flags
- Basic application settings
- **Strategy**: Inline storage in Orleans state

### Medium Configurations (1KB-100KB)
- Feature configurations with detailed settings
- User preference configurations
- Module-specific configurations
- **Strategy**: Compressed storage in Orleans state

### Large Configurations (> 100KB)
- ML model configurations
- Large dataset configurations
- Complex workflow definitions
- **Strategy**: External storage with Orleans state references

## 🧰 Test Utilities

### Test Data Generation
- `GenerateTestConfig(size)`: Creates realistic test configurations
- `CreateRepetitiveConfig(size)`: Creates highly compressible test data
- `GenerateRandomString(length)`: Generates random string data

### Performance Helpers
- Hash calculation utilities
- Compression/decompression helpers
- Memory usage measurement tools
- Timing and benchmark utilities

## 📈 Continuous Monitoring

### Performance Regression Detection
The test suite includes performance regression detection:

- Configuration update operations must complete within time limits
- Memory usage must stay within expected ranges
- Compression ratios must meet minimum thresholds
- Hash calculation performance must remain optimal

### Success Criteria
- ✅ All unit tests pass
- ✅ Performance benchmarks meet thresholds
- ✅ Memory usage stays within limits
- ✅ Compression achieves expected ratios
- ✅ Backward compatibility maintained

## 🔍 Debugging and Troubleshooting

### Common Issues
1. **Test Timeouts**: Increase timeout values in test configuration
2. **Memory Issues**: Ensure test isolation and proper cleanup
3. **Compression Failures**: Verify GZip stream handling
4. **Orleans State Issues**: Check event sourcing configuration

### Logging
Tests include comprehensive logging through `ITestOutputHelper`:
- Configuration sizes and strategies
- Compression ratios and performance metrics
- Hash calculations and change detection
- Error messages and diagnostic information

## 📚 Additional Resources

- [ConfigManagerGAgent Implementation](../../src/Aevatar.GAgents.Basic/BasicGAgents/ConfigManagerGAgent.cs)
- [Storage Service Interface](../../src/Aevatar.GAgents.Basic/BasicGAgents/ConfigManagerGAgent.cs#IConfigStorageService)
- [Performance Optimization Documentation](../../docs/)

---

*This test suite ensures the ConfigManagerGAgent Phase 1 optimizations deliver the expected performance improvements while maintaining full functionality and backward compatibility.*