# Agent Warmup System - End-to-End Test Plan

## Overview

This document outlines the comprehensive test plan for validating the Agent Warmup System in a real end-to-end testing environment. The tests focus on GUID-based agents and validate the system's ability to proactively load agents into memory while preventing MongoDB access spikes.

## Test Environment Setup

### Prerequisites
- Aevatar.Aspire project running (starts the silo) project Path: /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire
- MongoDB instance available
- Test agents deployed to the silo
- Agent warmup system configured and enabled

### Test Infrastructure
- **Test Location**: `aevatar-station/samples/AgentWarmupE2E/`
- **Silo Startup**: Via Aevatar.Aspire project or Orleans TestCluster
- **Agent Type**: GUID-based agents only (current system support)
- **Database**: MongoDB with isolated test database
- **Configuration**: Test-specific settings in `test-appsettings.json`
- **Test Agents**: Implemented in `E2E.Grains` project for single DLL grain deployment

## Test Architecture

### Test Project Structure
```
AgentWarmupE2E/
├── Tests/                          # Test implementations
│   ├── BasicFunctionalityTests.cs  # Core functionality validation
│   ├── PerformanceTests.cs         # Performance and latency tests
│   ├── MongoDbProtectionTests.cs   # MongoDB rate limiting tests
│   ├── ConfigurationTests.cs       # Configuration validation
│   ├── ErrorHandlingTests.cs       # Error scenarios
│   └── IntegrationTests.cs         # Orleans integration
├── Fixtures/                       # Test infrastructure
│   └── AgentWarmupTestFixture.cs   # Orleans TestCluster setup
├── Utilities/                      # Test utilities
│   ├── PerformanceMonitor.cs       # Performance measurement
│   ├── TestDataGenerator.cs        # GUID generation utilities
│   └── MongoDbMonitor.cs           # Database monitoring
├── Configuration/                  # Test configuration
│   └── test-appsettings.json       # Test-specific settings
├── GRAIN_WARMUP_TEST_PLAN.md       # This document
└── README.md                       # Test execution guide

E2E.Grains/                         # Shared test agent implementations
├── ITestWarmupAgent.cs             # GUID-based test agent interface
└── TestWarmupAgent.cs              # Test agent with tracking
```

### Test Agent Design

The test agents are implemented in the **E2E.Grains** project to allow the silo project to reference all grains from a single DLL. This approach ensures proper grain discovery and deployment.

#### ITestWarmupAgent Interface
```csharp
public interface ITestWarmupAgent : IGrainWithGuidKey
{
    Task<string> PingAsync();
    Task<DateTime> GetActivationTimeAsync();
    Task<int> ComputeAsync(int input);
    Task<int> GetAccessCountAsync();
    Task<string> SimulateDatabaseOperationAsync(int delayMs = 100);
    Task<AgentMetadata> GetMetadataAsync();
}
```

#### Key Features
- **GUID-based keys**: Uses `IGrainWithGuidKey` for current system compatibility
- **Activation tracking**: Records activation time and access patterns
- **Performance simulation**: Includes database operation simulation
- **Metadata collection**: Provides agent state information for validation
- **Shared deployment**: Located in E2E.Grains project for single DLL reference by silo

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
  1. Create PredefinedAgentWarmupStrategy with test GUIDs
  2. Register strategy with warmup service
  3. Verify strategy count increases
- **Expected Result**: Strategy registered successfully

##### TC-BF-003: Basic Agent Warmup
- **Objective**: Ensure agents are activated during warmup
- **Steps**:
  1. Generate 10 test GUID-based agent identifiers
  2. Register warmup strategy for these agents
  3. Start warmup process
  4. Wait for completion
  5. Verify all agents respond to ping
- **Expected Result**: All 10 agents warmed up successfully

##### TC-BF-004: Progress Monitoring
- **Objective**: Validate progress tracking accuracy
- **Steps**:
  1. Register strategy with known agent count
  2. Monitor progress during warmup
  3. Verify initial and final status
- **Expected Result**: Progress accurately reflects warmup state

##### TC-BF-005: Multiple Strategies
- **Objective**: Test sequential execution of multiple strategies
- **Steps**:
  1. Register two strategies with different agent sets
  2. Start warmup
  3. Verify both strategies execute
- **Expected Result**: Both strategies complete successfully

##### TC-BF-006: Graceful Cancellation
- **Objective**: Ensure warmup can be stopped gracefully
- **Steps**:
  1. Start warmup with large agent set
  2. Cancel after partial completion
  3. Verify graceful shutdown
- **Expected Result**: Warmup stops without errors

### 2. Performance Tests

#### Test Objectives
- Validate latency improvements from agent warmup
- Measure system throughput and efficiency
- Ensure performance meets defined thresholds
- Test system behavior under load

#### Performance Thresholds
- **Warmed Agent Latency**: < 50ms (P95)
- **Cold Agent Latency**: < 200ms (P95)
- **Latency Improvement**: > 50% reduction
- **Success Rate**: > 95%
- **Throughput**: > 20 agents/second

#### Test Cases

##### TC-PF-001: Activation Latency Comparison
- **Objective**: Demonstrate latency improvement from warmup
- **Steps**:
  1. Generate two sets of test agents (warmed vs cold)
  2. Warm up first set using warmup service
  3. Measure activation latency for both sets
  4. Compare results
- **Expected Result**: Warmed agents show significant latency improvement

##### TC-PF-002: Concurrent Access Performance
- **Objective**: Validate performance under concurrent load
- **Steps**:
  1. Warm up 100 test agents
  2. Access agents concurrently from 10 threads
  3. Measure latency and success rate
- **Expected Result**: Maintains low latency under concurrent access

##### TC-PF-003: Large Scale Warmup
- **Objective**: Test system with large agent counts
- **Steps**:
  1. Generate 1000+ test agent identifiers
  2. Execute warmup process
  3. Measure completion time and success rate
- **Expected Result**: Completes within time limit with high success rate

##### TC-PF-004: Throughput Validation
- **Objective**: Ensure minimum throughput requirements
- **Steps**:
  1. Warm up 200 agents
  2. Measure total time and calculate throughput
- **Expected Result**: Achieves minimum 20 agents/second throughput

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

##### TC-EH-001: Agent Activation Failures
- **Objective**: Handle individual agent activation failures
- **Steps**:
  1. Include invalid agent identifiers in warmup set
  2. Execute warmup process
  3. Verify system continues with valid agents
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
  2. Execute warmup with slow-responding agents
  3. Verify timeout handling
- **Expected Result**: Timeouts handled without system failure

##### TC-EH-004: Partial Failure Scenarios
- **Objective**: Test mixed success/failure scenarios
- **Steps**:
  1. Create warmup set with mix of valid/invalid agents
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
var basicAgents = TestDataGenerator.TestCategories.GenerateBasicTestAgents(count);

// Performance tests
var perfAgents = TestDataGenerator.TestCategories.GeneratePerformanceTestAgents(count);

// Warmup-specific tests
var warmupAgents = TestDataGenerator.TestCategories.GenerateWarmupTestAgents(count);

// Cold agent tests
var coldAgents = TestDataGenerator.TestCategories.GenerateColdTestAgents(count);

// Error handling tests
var errorAgents = TestDataGenerator.TestCategories.GenerateErrorTestAgents(count);

// Large-scale tests
var largeAgents = TestDataGenerator.TestCategories.GenerateLargeScaleTestAgents(count);
```

### Data Isolation
- Each test category uses distinct GUID prefixes
- Test runs use isolated MongoDB databases
- Agent activations cleared between test categories

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
- ✅ Agent warmup reduces activation latency by >50%
- ✅ System handles 1000+ agents within time limits
- ✅ MongoDB rate limiting prevents access spikes
- ✅ Error scenarios handled gracefully
- ✅ Configuration validation works correctly

### Performance Requirements
- ✅ Warmed agent latency < 50ms (P95)
- ✅ Cold agent latency < 200ms (P95)
- ✅ Throughput > 20 agents/second
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
- **Risk**: System doesn't scale to production agent counts
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

This comprehensive test plan ensures the Agent Warmup System meets all functional, performance, and quality requirements. The test suite provides:

1. **Complete Coverage**: All system components and scenarios tested
2. **Performance Validation**: Latency improvements and throughput verified
3. **Reliability Assurance**: Error handling and resilience validated
4. **Integration Verification**: Orleans and MongoDB integration confirmed
5. **Quality Metrics**: Coverage, performance, and stability measured

The test implementation provides a robust foundation for validating the agent warmup system in both development and production environments, ensuring it delivers the expected benefits of reduced agent activation latency and MongoDB protection. 