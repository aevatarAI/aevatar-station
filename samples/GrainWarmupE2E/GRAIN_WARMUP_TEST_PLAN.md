# Grain Warmup System - End-to-End Test Plan

## Overview

This document outlines the comprehensive test plan for validating the Grain Warmup System in a real end-to-end testing environment. The tests focus on GUID-based grains and validate the system's ability to proactively load grains into memory while preventing MongoDB access spikes.

## Test Environment Setup

### Prerequisites
- Aevatar.Aspire project running (starts the silo) project Path: /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire
- MongoDB instance available
- Test grains deployed to the silo
- Grain warmup system configured and enabled

### Test Infrastructure
- **Test Location**: `aevatar-station/samples/GrainWarmupE2E/`
- **Silo Startup**: Via Aevatar.Aspire project or Orleans TestCluster
- **Grain Type**: GUID-based grains only (current system support)
- **Database**: MongoDB with isolated test database
- **Configuration**: Test-specific settings in `test-appsettings.json`

## Test Architecture

### Test Project Structure
```
GrainWarmupE2E/
├── Tests/                          # Test implementations
│   ├── BasicFunctionalityTests.cs  # Core functionality validation
│   ├── PerformanceTests.cs         # Performance and latency tests
│   ├── MongoDbProtectionTests.cs   # MongoDB rate limiting tests
│   ├── ConfigurationTests.cs       # Configuration validation
│   ├── ErrorHandlingTests.cs       # Error scenarios
│   └── IntegrationTests.cs         # Orleans integration
├── TestGrains/                     # Test grain implementations
│   ├── ITestWarmupGrain.cs         # GUID-based test grain interface
│   └── TestWarmupGrain.cs          # Test grain with tracking
├── Fixtures/                       # Test infrastructure
│   └── GrainWarmupTestFixture.cs   # Orleans TestCluster setup
├── Utilities/                      # Test utilities
│   ├── PerformanceMonitor.cs       # Performance measurement
│   ├── TestDataGenerator.cs        # GUID generation utilities
│   └── MongoDbMonitor.cs           # Database monitoring
├── Configuration/                  # Test configuration
│   └── test-appsettings.json       # Test-specific settings
├── GRAIN_WARMUP_TEST_PLAN.md       # This document
└── README.md                       # Test execution guide
```

### Test Grain Design

#### ITestWarmupGrain Interface
```csharp
public interface ITestWarmupGrain : IGrainWithGuidKey
{
    Task<string> PingAsync();
    Task<DateTime> GetActivationTimeAsync();
    Task<int> ComputeAsync(int input);
    Task<int> GetAccessCountAsync();
    Task<string> SimulateDatabaseOperationAsync(int delayMs = 100);
    Task<GrainMetadata> GetMetadataAsync();
}
```

#### Key Features
- **GUID-based keys**: Uses `IGrainWithGuidKey` for current system compatibility
- **Activation tracking**: Records activation time and access patterns
- **Performance simulation**: Includes database operation simulation
- **Metadata collection**: Provides grain state information for validation

## Test Categories and Scenarios

### 1. Basic Functionality Tests

#### Test Objectives
- Validate core warmup system functionality
- Ensure proper service initialization
- Verify strategy registration and execution
- Test progress monitoring and status reporting

#### Test Cases

##### TC-BF-001: Service Initialization
- **Objective**: Verify warmup service starts correctly with silo
- **Steps**:
  1. Initialize Orleans TestCluster with warmup system
  2. Retrieve warmup service from DI container
  3. Verify service is not null and status is accessible
- **Expected Result**: Service initializes successfully and provides status

##### TC-BF-002: Strategy Registration
- **Objective**: Validate strategy registration mechanism
- **Steps**:
  1. Create PredefinedGrainWarmupStrategy with test GUIDs
  2. Register strategy with warmup service
  3. Verify strategy count increases
- **Expected Result**: Strategy registered successfully

##### TC-BF-003: Basic Grain Warmup
- **Objective**: Ensure grains are activated during warmup
- **Steps**:
  1. Generate 10 test GUID-based grain identifiers
  2. Register warmup strategy for these grains
  3. Start warmup process
  4. Wait for completion
  5. Verify all grains respond to ping
- **Expected Result**: All 10 grains warmed up successfully

##### TC-BF-004: Progress Monitoring
- **Objective**: Validate progress tracking accuracy
- **Steps**:
  1. Register strategy with known grain count
  2. Monitor progress during warmup
  3. Verify initial and final status
- **Expected Result**: Progress accurately reflects warmup state

##### TC-BF-005: Multiple Strategies
- **Objective**: Test sequential execution of multiple strategies
- **Steps**:
  1. Register two strategies with different grain sets
  2. Start warmup
  3. Verify both strategies execute
- **Expected Result**: Both strategies complete successfully

##### TC-BF-006: Graceful Cancellation
- **Objective**: Ensure warmup can be stopped gracefully
- **Steps**:
  1. Start warmup with large grain set
  2. Cancel after partial completion
  3. Verify graceful shutdown
- **Expected Result**: Warmup stops without errors

### 2. Performance Tests

#### Test Objectives
- Validate latency improvements from grain warmup
- Measure system throughput and efficiency
- Ensure performance meets defined thresholds
- Test system behavior under load

#### Performance Thresholds
- **Warmed Grain Latency**: < 50ms (P95)
- **Cold Grain Latency**: < 200ms (P95)
- **Latency Improvement**: > 50% reduction
- **Success Rate**: > 95%
- **Throughput**: > 20 grains/second

#### Test Cases

##### TC-PF-001: Activation Latency Comparison
- **Objective**: Demonstrate latency improvement from warmup
- **Steps**:
  1. Generate two sets of test grains (warmed vs cold)
  2. Warm up first set using warmup service
  3. Measure activation latency for both sets
  4. Compare results
- **Expected Result**: Warmed grains show significant latency improvement

##### TC-PF-002: Concurrent Access Performance
- **Objective**: Validate performance under concurrent load
- **Steps**:
  1. Warm up 100 test grains
  2. Access grains concurrently from 10 threads
  3. Measure latency and success rate
- **Expected Result**: Maintains low latency under concurrent access

##### TC-PF-003: Large Scale Warmup
- **Objective**: Test system with large grain counts
- **Steps**:
  1. Generate 1000+ test grain identifiers
  2. Execute warmup process
  3. Measure completion time and success rate
- **Expected Result**: Completes within time limit with high success rate

##### TC-PF-004: Throughput Validation
- **Objective**: Ensure minimum throughput requirements
- **Steps**:
  1. Warm up 200 grains
  2. Measure total time and calculate throughput
- **Expected Result**: Achieves minimum 20 grains/second throughput

##### TC-PF-005: Progressive Warmup Efficiency
- **Objective**: Validate progressive batching improves efficiency
- **Steps**:
  1. Monitor warmup progress over time
  2. Calculate throughput for early vs late phases
- **Expected Result**: Later phases show improved efficiency

##### TC-PF-006: Memory Usage Stability
- **Objective**: Ensure no memory leaks during warmup
- **Steps**:
  1. Measure initial memory usage
  2. Execute large-scale warmup
  3. Force garbage collection and measure final memory
- **Expected Result**: Memory increase within acceptable bounds

### 3. MongoDB Protection Tests

#### Test Objectives
- Validate MongoDB rate limiting functionality
- Ensure connection pool protection
- Test progressive batch size increases
- Verify burst handling capabilities

#### Test Cases

##### TC-MP-001: Rate Limiting Enforcement
- **Objective**: Ensure MongoDB operations respect rate limits
- **Steps**:
  1. Configure low rate limit (5 ops/sec)
  2. Monitor actual operation rate during warmup
  3. Verify rate stays within limits
- **Expected Result**: Operation rate respects configured limits

##### TC-MP-002: Connection Pool Protection
- **Objective**: Prevent connection pool exhaustion
- **Steps**:
  1. Configure limited connection pool
  2. Execute concurrent warmup operations
  3. Monitor connection usage
- **Expected Result**: Connection usage stays within pool limits

##### TC-MP-003: Progressive Batching
- **Objective**: Validate gradual batch size increase
- **Steps**:
  1. Monitor batch sizes during warmup
  2. Verify progressive increase pattern
- **Expected Result**: Batch sizes increase gradually as configured

##### TC-MP-004: Burst Handling
- **Objective**: Test system response to burst requests
- **Steps**:
  1. Configure burst allowance
  2. Generate burst of warmup requests
  3. Verify proper handling
- **Expected Result**: Burst handled within configured limits

### 4. Configuration Tests

#### Test Objectives
- Validate configuration parameter handling
- Test invalid configuration scenarios
- Verify runtime configuration changes
- Ensure proper defaults

#### Test Cases

##### TC-CF-001: Configuration Validation
- **Objective**: Ensure invalid configurations are rejected
- **Steps**:
  1. Provide invalid configuration values
  2. Attempt to initialize warmup service
  3. Verify appropriate error handling
- **Expected Result**: Invalid configurations rejected with clear errors

##### TC-CF-002: Default Configuration
- **Objective**: Verify system works with default settings
- **Steps**:
  1. Initialize system without explicit configuration
  2. Verify default values are applied
- **Expected Result**: System operates with sensible defaults

##### TC-CF-003: Runtime Configuration Changes
- **Objective**: Test hot configuration reload
- **Steps**:
  1. Start system with initial configuration
  2. Update configuration at runtime
  3. Verify changes take effect
- **Expected Result**: Configuration changes applied without restart

### 5. Error Handling Tests

#### Test Objectives
- Validate system resilience to failures
- Test error recovery mechanisms
- Ensure graceful degradation
- Verify proper error reporting

#### Test Cases

##### TC-EH-001: Grain Activation Failures
- **Objective**: Handle individual grain activation failures
- **Steps**:
  1. Include invalid grain identifiers in warmup set
  2. Execute warmup process
  3. Verify system continues with valid grains
- **Expected Result**: System handles failures gracefully

##### TC-EH-002: Database Connectivity Issues
- **Objective**: Test behavior during database outages
- **Steps**:
  1. Start warmup process
  2. Simulate database connectivity loss
  3. Verify error handling and recovery
- **Expected Result**: System handles database issues appropriately

##### TC-EH-003: Timeout Handling
- **Objective**: Ensure proper timeout behavior
- **Steps**:
  1. Configure short timeouts
  2. Execute warmup with slow-responding grains
  3. Verify timeout handling
- **Expected Result**: Timeouts handled without system failure

##### TC-EH-004: Partial Failure Scenarios
- **Objective**: Test mixed success/failure scenarios
- **Steps**:
  1. Create warmup set with mix of valid/invalid grains
  2. Execute warmup
  3. Verify partial success handling
- **Expected Result**: System reports accurate success/failure counts

### 6. Integration Tests

#### Test Objectives
- Validate Orleans integration
- Test dependency injection
- Verify logging integration
- Ensure proper lifecycle management

#### Test Cases

##### TC-IT-001: Orleans Lifecycle Integration
- **Objective**: Ensure proper integration with Orleans lifecycle
- **Steps**:
  1. Start Orleans silo with warmup system
  2. Verify warmup service participates in lifecycle
  3. Test graceful shutdown
- **Expected Result**: Proper lifecycle integration

##### TC-IT-002: Dependency Injection
- **Objective**: Validate DI container integration
- **Steps**:
  1. Register warmup services in DI container
  2. Resolve services and verify dependencies
- **Expected Result**: All dependencies resolved correctly

##### TC-IT-003: Logging Integration
- **Objective**: Ensure proper logging throughout system
- **Steps**:
  1. Configure logging providers
  2. Execute warmup operations
  3. Verify log output
- **Expected Result**: Comprehensive logging at appropriate levels

## Test Data Management

### GUID Generation Strategy
- **Deterministic**: Uses fixed seeds for reproducible results
- **Categorized**: Different prefixes for test categories
- **Collision-Free**: Ensures unique identifiers across test runs

### Test Data Categories
```csharp
// Basic functionality tests
var basicGrains = TestDataGenerator.TestCategories.GenerateBasicTestGrains(count);

// Performance tests
var perfGrains = TestDataGenerator.TestCategories.GeneratePerformanceTestGrains(count);

// Warmup-specific tests
var warmupGrains = TestDataGenerator.TestCategories.GenerateWarmupTestGrains(count);

// Cold grain tests
var coldGrains = TestDataGenerator.TestCategories.GenerateColdTestGrains(count);

// Error handling tests
var errorGrains = TestDataGenerator.TestCategories.GenerateErrorTestGrains(count);

// Large-scale tests
var largeGrains = TestDataGenerator.TestCategories.GenerateLargeScaleTestGrains(count);
```

### Data Isolation
- Each test category uses distinct GUID prefixes
- Test runs use isolated MongoDB databases
- Grain activations cleared between test categories

## Performance Monitoring

### Metrics Collection
The test suite collects comprehensive performance metrics:

#### Latency Metrics
- **Average Latency**: Mean response time
- **Median Latency**: 50th percentile
- **P95 Latency**: 95th percentile
- **P99 Latency**: 99th percentile
- **Min/Max Latency**: Range of response times

#### Throughput Metrics
- **Operations per Second**: Warmup rate
- **Concurrent Operations**: Parallel processing capability
- **Batch Processing Rate**: Efficiency of batching

#### System Metrics
- **Memory Usage**: Heap allocation patterns
- **CPU Utilization**: Processing overhead
- **Network I/O**: Communication patterns
- **Database Connections**: Connection pool usage

### Performance Reporting
Tests generate detailed reports including:
- Statistical analysis of latency distributions
- Throughput trends over time
- Resource utilization patterns
- Comparison with baseline metrics

## Test Execution

### Automated Test Execution
```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=BasicFunctionality"
dotnet test --filter "Category=Performance"
dotnet test --filter "Category=MongoDbProtection"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Manual Test Execution
For integration with Aevatar.Aspire:
1. Start Aevatar.Aspire project
2. Configure test client to connect to running silo
3. Execute specific test scenarios
4. Monitor warmup behavior in production environment

### Continuous Integration
The test suite integrates with CI/CD pipelines:
- Automated test execution on code changes
- Performance regression detection
- Test result reporting and analysis
- Coverage tracking and reporting

## Success Criteria

### Functional Requirements
- ✅ All basic functionality tests pass
- ✅ Grain warmup reduces activation latency by >50%
- ✅ System handles 1000+ grains within time limits
- ✅ MongoDB rate limiting prevents access spikes
- ✅ Error scenarios handled gracefully
- ✅ Configuration validation works correctly

### Performance Requirements
- ✅ Warmed grain latency < 50ms (P95)
- ✅ Cold grain latency < 200ms (P95)
- ✅ Throughput > 20 grains/second
- ✅ Success rate > 95%
- ✅ Memory usage within acceptable bounds
- ✅ No connection pool exhaustion

### Quality Requirements
- ✅ Test coverage > 80%
- ✅ No memory leaks detected
- ✅ Proper error handling and logging
- ✅ Configuration validation comprehensive
- ✅ Documentation complete and accurate

## Risk Assessment and Mitigation

### Identified Risks

#### Performance Risks
- **Risk**: Warmup process impacts system performance
- **Mitigation**: Rate limiting and progressive batching
- **Test Coverage**: Performance tests validate impact

#### Reliability Risks
- **Risk**: Warmup failures affect system stability
- **Mitigation**: Error isolation and graceful degradation
- **Test Coverage**: Error handling tests validate resilience

#### Scalability Risks
- **Risk**: System doesn't scale to production grain counts
- **Mitigation**: Large-scale testing and optimization
- **Test Coverage**: Large-scale tests validate scalability

#### Integration Risks
- **Risk**: Poor integration with Orleans or MongoDB
- **Mitigation**: Comprehensive integration testing
- **Test Coverage**: Integration tests validate compatibility

### Mitigation Strategies
1. **Comprehensive Testing**: Cover all scenarios and edge cases
2. **Performance Monitoring**: Continuous performance validation
3. **Error Handling**: Robust error recovery mechanisms
4. **Configuration Validation**: Prevent invalid configurations
5. **Documentation**: Clear usage guidelines and troubleshooting

## Test Environment Requirements

### Hardware Requirements
- **CPU**: Multi-core processor for concurrent testing
- **Memory**: Minimum 8GB RAM for large-scale tests
- **Storage**: SSD recommended for database performance
- **Network**: Low-latency network for Orleans communication

### Software Requirements
- **.NET 9.0 SDK**: Latest runtime and development tools
- **MongoDB**: Database server for persistence testing
- **Visual Studio/VS Code**: Development environment
- **Git**: Version control for test code management

### Environment Configuration
- **Test Isolation**: Separate test databases and configurations
- **Resource Limits**: Appropriate limits for test scenarios
- **Monitoring**: Performance and resource monitoring tools
- **Logging**: Comprehensive logging for debugging

## Conclusion

This comprehensive test plan ensures the Grain Warmup System meets all functional, performance, and quality requirements. The test suite provides:

1. **Complete Coverage**: All system components and scenarios tested
2. **Performance Validation**: Latency improvements and throughput verified
3. **Reliability Assurance**: Error handling and resilience validated
4. **Integration Verification**: Orleans and MongoDB integration confirmed
5. **Quality Metrics**: Coverage, performance, and stability measured

The test implementation provides a robust foundation for validating the grain warmup system in both development and production environments, ensuring it delivers the expected benefits of reduced grain activation latency and MongoDB protection. 