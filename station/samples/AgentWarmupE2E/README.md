# Grain Warmup System - End-to-End Tests

This project contains comprehensive end-to-end tests for the Grain Warmup System, designed to validate functionality, performance, and reliability in a real Orleans environment.

## Overview

The test suite validates the grain warmup system's ability to:
- Proactively load grains into memory
- Reduce grain activation latency
- Protect MongoDB from access spikes
- Handle various configuration scenarios
- Maintain system stability under load

## Test Structure

```
GrainWarmupE2E/
├── Tests/                          # Test classes
│   ├── BasicFunctionalityTests.cs  # Core functionality validation
│   ├── PerformanceTests.cs         # Performance and latency tests
│   ├── MongoDbProtectionTests.cs   # MongoDB rate limiting tests
│   ├── ConfigurationTests.cs       # Configuration validation tests
│   ├── ErrorHandlingTests.cs       # Error scenarios and resilience
│   └── IntegrationTests.cs         # Orleans integration tests
├── TestGrains/                     # Test grain implementations
│   ├── ITestWarmupGrain.cs         # Test grain interface
│   └── TestWarmupGrain.cs          # Test grain implementation
├── Fixtures/                       # Test infrastructure
│   └── GrainWarmupTestFixture.cs   # Orleans test cluster setup
├── Utilities/                      # Test utilities
│   ├── PerformanceMonitor.cs       # Performance measurement
│   ├── TestDataGenerator.cs        # Test data generation
│   └── MongoDbMonitor.cs           # MongoDB monitoring
└── Configuration/                  # Test configuration
    └── test-appsettings.json       # Test-specific settings
```

## Prerequisites

### Software Requirements
- .NET 9.0 SDK
- MongoDB (for database integration tests)
- Visual Studio 2022 or VS Code with C# extension

### Environment Setup
1. **MongoDB**: Ensure MongoDB is running on `localhost:27017`
2. **Orleans Silo**: Tests use Orleans TestCluster (no external silo required)
3. **Test Database**: Tests use isolated database `GrainWarmupE2ETest`

## Running the Tests

### Command Line
```bash
# Navigate to the test project directory
cd aevatar-station/samples/GrainWarmupE2E

# Restore dependencies
dotnet restore

# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=BasicFunctionality"
dotnet test --filter "Category=Performance"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio
1. Open the solution in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test → Test Explorer)
4. Run all tests or select specific tests

### VS Code
1. Open the project folder in VS Code
2. Install the C# extension if not already installed
3. Use the integrated terminal to run `dotnet test`
4. Or use the Test Explorer extension

## Test Categories

### 1. Basic Functionality Tests
**File**: `Tests/BasicFunctionalityTests.cs`

Tests core warmup system functionality:
- Service initialization and registration
- Strategy registration and execution
- Grain activation tracking
- Progress monitoring
- Multiple strategy execution
- Graceful cancellation

**Key Tests**:
- `ServiceInitialization_ShouldStartCorrectlyWithSilo`
- `BasicGrainWarmup_ShouldActivateAllGrains`
- `WarmupStatus_ShouldProvideAccurateProgress`

### 2. Performance Tests
**File**: `Tests/PerformanceTests.cs`

Validates performance improvements and system efficiency:
- Latency comparison (warmed vs cold grains)
- Concurrent access performance
- Large-scale warmup throughput
- Progressive warmup efficiency
- Memory usage stability

**Key Tests**:
- `ActivationLatencyComparison_WarmedGrainsShouldBeFaster`
- `ConcurrentAccessPerformance_ShouldMaintainLowLatency`
- `LargeScaleWarmup_ShouldCompleteWithinTimeLimit`

**Performance Thresholds** (configurable in `test-appsettings.json`):
- Warmed grain latency: < 50ms
- Cold grain latency: < 200ms
- Latency improvement: > 50%
- Success rate: > 95%

### 3. MongoDB Protection Tests
**File**: `Tests/MongoDbProtectionTests.cs`

Validates MongoDB rate limiting and connection protection:
- Rate limiting enforcement
- Connection pool protection
- Progressive batch size increase
- Burst handling

**Key Tests**:
- `RateLimiting_ShouldRespectMongoDbLimits`
- `ConnectionPoolProtection_ShouldNotExhaustConnections`
- `ProgressiveBatching_ShouldIncreaseGradually`

### 4. Configuration Tests
**File**: `Tests/ConfigurationTests.cs`

Tests configuration validation and hot reload:
- Configuration validation
- Invalid parameter handling
- Runtime configuration changes
- Strategy-specific configuration

### 5. Error Handling Tests
**File**: `Tests/ErrorHandlingTests.cs`

Validates system resilience and error recovery:
- Grain activation failures
- Database connectivity issues
- Timeout handling
- Partial failure scenarios

### 6. Integration Tests
**File**: `Tests/IntegrationTests.cs`

Tests Orleans integration and lifecycle management:
- Silo lifecycle integration
- Dependency injection
- Logging integration
- Event handling

## Test Configuration

### Test Settings
The `test-appsettings.json` file contains test-specific configuration:

```json
{
  "GrainWarmup": {
    "Enabled": true,
    "MaxConcurrency": 5,
    "BatchDelayMs": 50,
    "InitialBatchSize": 2,
    "MaxBatchSize": 10,
    "MongoDbRateLimit": {
      "MaxOperationsPerSecond": 20,
      "TimeWindowMs": 1000
    }
  },
  "TestConfiguration": {
    "TestGrainCount": 100,
    "PerformanceTestGrainCount": 1000,
    "LatencyTestIterations": 50,
    "TestTimeoutMs": 30000,
    "PerformanceThresholds": {
      "ColdGrainMaxLatencyMs": 200,
      "WarmedGrainMaxLatencyMs": 50,
      "LatencyImprovementRatio": 0.5,
      "SuccessRateThreshold": 0.95
    }
  }
}
```

### Customizing Test Parameters
You can modify test behavior by updating the configuration:

1. **Grain Counts**: Adjust `TestGrainCount` for different test scales
2. **Performance Thresholds**: Modify latency and success rate expectations
3. **Timeouts**: Adjust test timeouts for slower environments
4. **Rate Limits**: Change MongoDB rate limiting for different scenarios

## Test Data Generation

The test suite uses deterministic data generation for reproducible results:

### GUID Generation
- **Deterministic**: Uses fixed seeds for consistent test results
- **Categorized**: Different prefixes for different test types
- **Collision-Free**: Ensures unique identifiers across test runs

### Test Categories
- `basic`: Basic functionality tests
- `perf`: Performance tests
- `warmup`: Warmup-specific tests
- `cold`: Cold grain tests
- `error`: Error handling tests
- `large`: Large-scale tests

## Performance Monitoring

The test suite includes comprehensive performance monitoring:

### Metrics Collected
- **Latency**: Average, median, P95, P99
- **Throughput**: Operations per second
- **Success Rate**: Percentage of successful operations
- **Memory Usage**: Memory consumption patterns
- **Resource Usage**: CPU and network utilization

### Performance Reports
Tests generate detailed performance reports including:
- Operation statistics
- Latency distributions
- Throughput analysis
- Resource utilization
- Trend analysis

## Troubleshooting

### Common Issues

#### 1. MongoDB Connection Errors
```
Error: Unable to connect to MongoDB
```
**Solution**: Ensure MongoDB is running on `localhost:27017`

#### 2. Test Timeouts
```
Error: Test timed out after 30 seconds
```
**Solution**: Increase timeout values in `test-appsettings.json`

#### 3. Memory Issues
```
Error: OutOfMemoryException during large-scale tests
```
**Solution**: Reduce test grain counts or increase available memory

#### 4. Port Conflicts
```
Error: Address already in use
```
**Solution**: Ensure no other Orleans silos are running

### Debug Mode
Enable detailed logging by setting log level to `Debug`:

```json
{
  "Logging": {
    "LogLevel": {
      "GrainWarmupE2E": "Debug",
      "Aevatar.Silo.GrainWarmup": "Debug"
    }
  }
}
```

### Performance Debugging
For performance issues:
1. Enable performance counters
2. Use profiling tools (dotTrace, PerfView)
3. Monitor system resources during tests
4. Analyze performance reports

## Integration with Aevatar.Aspire

While these tests use Orleans TestCluster for isolation, you can also test against a real Aevatar.Aspire deployment:

### Manual Testing Steps
1. Start Aevatar.Aspire project
2. Modify test configuration to use external silo
3. Run specific test scenarios
4. Monitor warmup behavior in production-like environment

### Configuration for External Silo
```json
{
  "Orleans": {
    "ClusterId": "AevatarCluster",
    "ServiceId": "AevatarService",
    "Clustering": {
      "Provider": "MongoDB",
      "ConnectionString": "mongodb://localhost:27017"
    }
  }
}
```

## Continuous Integration

The test suite is designed for CI/CD integration:

### GitHub Actions Example
```yaml
- name: Run Grain Warmup E2E Tests
  run: |
    cd aevatar-station/samples/GrainWarmupE2E
    dotnet test --logger trx --results-directory TestResults
    
- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Grain Warmup E2E Tests
    path: '**/TestResults/*.trx'
    reporter: dotnet-trx
```

### Test Reporting
- **Test Results**: Standard xUnit XML format
- **Coverage Reports**: Cobertura format
- **Performance Metrics**: JSON format for analysis
- **Logs**: Structured logging for debugging

## Contributing

When adding new tests:

1. **Follow Naming Conventions**: Use descriptive test names
2. **Add Documentation**: Include test purpose and expected behavior
3. **Use Test Categories**: Categorize tests appropriately
4. **Include Performance Metrics**: Add relevant performance validation
5. **Update Configuration**: Add new configuration options if needed
6. **Maintain Isolation**: Ensure tests don't interfere with each other

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review test logs for detailed error information
3. Consult the main grain warmup system documentation
4. Create an issue with test results and configuration details 