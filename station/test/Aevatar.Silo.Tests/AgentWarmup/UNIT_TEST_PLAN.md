# Agent Warmup System - Unit Test Plan

## Overview
This document outlines the comprehensive unit test plan for the Agent Warmup System. The plan covers all components, strategies, services, and edge cases that require unit testing to ensure robust functionality and maintainability.

## Test Coverage Status
- **Current Coverage**: ~5% (Integration tests only)
- **Target Coverage**: 90%+
- **Priority**: High (Critical system component)

## Test Categories

### 1. Strategy Classes

#### 1.1 SampleBasedAgentWarmupStrategy<TIdentifier>
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/SampleBasedAgentWarmupStrategyTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateStrategyWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenNameIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenAgentTypeIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenMongoDbServiceIsNull()`
- ✅ `ShouldThrowArgumentException_WhenSampleRatioIsZero()`
- ✅ `ShouldThrowArgumentException_WhenSampleRatioIsNegative()`
- ✅ `ShouldThrowArgumentException_WhenSampleRatioIsGreaterThanOne()`
- ✅ `ShouldThrowArgumentException_WhenBatchSizeIsZeroOrNegative()`
- ✅ `ShouldAcceptValidSampleRatio_BoundaryValues()`

**Property Tests:**
- ✅ `ShouldReturnCorrectName()`
- ✅ `ShouldReturnCorrectApplicableAgentTypes()`
- ✅ `ShouldReturnCorrectPriority()`
- ✅ `ShouldReturnEstimatedAgentCount()`

**Sampling Algorithm Tests:**
- ✅ `ShouldSampleCorrectPercentage_WithVariousRatios()`
- ✅ `ShouldReturnAllIdentifiers_WhenSampleRatioIsOne()`
- ✅ `ShouldReturnAtLeastOneIdentifier_WhenSampleRatioIsVerySmall()`
- ✅ `ShouldProduceDeterministicResults_WithSameSeed()`
- ✅ `ShouldProduceDifferentResults_WithDifferentSeeds()`
- ✅ `ShouldHandleEmptyIdentifierList()`
- ✅ `ShouldHandleSingleIdentifier()`
- ✅ `ShouldHandleLargeIdentifierList()`

**Fisher-Yates Shuffle Tests:**
- ✅ `ShouldShuffleIdentifiersRandomly()`
- ✅ `ShouldNotModifyOriginalList()`
- ✅ `ShouldReturnCorrectSampleSize()`

**MongoDB Integration Tests:**
- ✅ `ShouldRetrieveIdentifiersFromMongoDB()`
- ✅ `ShouldHandleMongoDBConnectionFailure()`
- ✅ `ShouldHandleEmptyMongoDBCollection()`
- ✅ `ShouldRespectCancellationToken()`

**Batch Processing Tests:**
- ✅ `ShouldAddDelayAfterBatchSize()`
- ✅ `ShouldRespectCustomBatchSize()`
- ✅ `ShouldHandleCancellationDuringBatchProcessing()`

**Error Handling Tests:**
- ✅ `ShouldLogWarning_WhenNoIdentifiersFound()`
- ✅ `ShouldHandleMongoDBTimeout()`
- ✅ `ShouldHandleInvalidIdentifierTypes()`

#### 1.2 DefaultAgentWarmupStrategy<TIdentifier>
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/DefaultAgentWarmupStrategyTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateStrategyWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenMongoDbServiceIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenConfigurationIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenLoggerIsNull()`

**Property Tests:**
- ✅ `ShouldReturnDefaultStrategyName()`
- ✅ `ShouldReturnEmptyApplicableAgentTypes()`
- ✅ `ShouldReturnLowestPriority()`

**Identifier Generation Tests:**
- ✅ `ShouldGenerateIdentifiersFromMongoDB()`
- ✅ `ShouldRespectMaxIdentifiersPerTypeConfiguration()`
- ✅ `ShouldHandleEmptyMongoDBCollection()`
- ✅ `ShouldRespectCancellationToken()`

**Configuration Tests:**
- ✅ `ShouldUseConfigurationMaxIdentifiers()`
- ✅ `ShouldHandleInvalidConfiguration()`
- ✅ `ShouldUseDefaultValues_WhenConfigurationMissing()`

**Error Handling Tests:**
- ✅ `ShouldHandleMongoDBConnectionFailure()`
- ✅ `ShouldLogErrors_WhenIdentifierRetrievalFails()`

#### 1.3 PredefinedAgentWarmupStrategy<TIdentifier>
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/PredefinedAgentWarmupStrategyTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateStrategyWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenNameIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenAgentTypeIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenIdentifiersIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenLoggerIsNull()`
- ✅ `ShouldAcceptEmptyIdentifiersList()`

**Property Tests:**
- ✅ `ShouldReturnCorrectName()`
- ✅ `ShouldReturnCorrectAgentType()`
- ✅ `ShouldReturnHighPriority()`
- ✅ `ShouldReturnCorrectEstimatedCount()`

**Identifier Generation Tests:**
- ✅ `ShouldReturnPredefinedIdentifiers()`
- ✅ `ShouldReturnIdentifiersInOrder()`
- ✅ `ShouldHandleEmptyIdentifiersList()`
- ✅ `ShouldRespectCancellationToken()`

**Type Matching Tests:**
- ✅ `ShouldApplyToCorrectAgentType()`
- ✅ `ShouldNotApplyToIncorrectAgentType()`

#### 1.4 BaseAgentWarmupStrategy<TIdentifier>
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/BaseAgentWarmupStrategyTests.cs`

**Abstract Implementation Tests:**
- ✅ `ShouldImplementIAgentWarmupStrategy()`
- ✅ `ShouldProvideDefaultCreateAgentReference()`
- ✅ `ShouldHandleGuidIdentifiers()`
- ✅ `ShouldHandleStringIdentifiers()`
- ✅ `ShouldHandleLongIdentifiers()`
- ✅ `ShouldHandleIntIdentifiers()`

**Agent Creation Tests:**
- ✅ `ShouldCreateCorrectAgentReference_ForGuidKey()`
- ✅ `ShouldCreateCorrectAgentReference_ForStringKey()`
- ✅ `ShouldCreateCorrectAgentReference_ForLongKey()`
- ✅ `ShouldCreateCorrectAgentReference_ForIntKey()`
- ✅ `ShouldThrowException_ForUnsupportedIdentifierType()`

**Interface Compatibility Tests:**
- ✅ `ShouldCheckStringKeyCompatibility()`
- ✅ `ShouldDetectIncompatibleInterfaces()`

**Error Handling Tests:**
- ✅ `ShouldHandleNullAgentFactory()`
- ✅ `ShouldHandleInvalidAgentInterface()`

### 2. Core Service Classes

#### 2.1 MongoDbAgentIdentifierService
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/MongoDbAgentIdentifierServiceTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateServiceWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenDatabaseIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenConfigurationIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenLoggerIsNull()`

**Collection Naming Tests:**
- ✅ `ShouldGenerateCorrectCollectionName_FullTypeName()`
- ✅ `ShouldGenerateCorrectCollectionName_TypeName()`
- ✅ `ShouldGenerateCorrectCollectionName_Custom()`
- ✅ `ShouldApplyCollectionPrefix_Automatically()`
- ✅ `ShouldApplyCollectionPrefix_Manual()`
- ✅ `ShouldHandleHostIdInPrefix()`

**Identifier Retrieval Tests:**
- ✅ `ShouldRetrieveGuidIdentifiers()`
- ✅ `ShouldRetrieveStringIdentifiers()`
- ✅ `ShouldRetrieveLongIdentifiers()`
- ✅ `ShouldRetrieveIntIdentifiers()`
- ✅ `ShouldRespectMaxCountParameter()`
- ✅ `ShouldHandleEmptyCollection()`
- ✅ `ShouldRespectCancellationToken()`

**Document ID Parsing Tests:**
- ✅ `ShouldParseGuidFromDocumentId()`
- ✅ `ShouldParseStringFromDocumentId()`
- ✅ `ShouldParseLongFromDocumentId()`
- ✅ `ShouldParseIntFromDocumentId()`
- ✅ `ShouldSkipInvalidDocumentIds()`
- ✅ `ShouldLogParsingErrors()`

**Collection Management Tests:**
- ✅ `ShouldCheckCollectionExists()`
- ✅ `ShouldReturnFalse_WhenCollectionDoesNotExist()`
- ✅ `ShouldGetCorrectAgentCount()`
- ✅ `ShouldReturnZero_WhenCollectionEmpty()`

**Error Handling Tests:**
- ✅ `ShouldHandleMongoDBConnectionFailure()`
- ✅ `ShouldHandleQueryTimeout()`
- ✅ `ShouldHandleInvalidDocumentFormat()`
- ✅ `ShouldLogConnectionErrors()`

**Configuration Tests:**
- ✅ `ShouldUseConfiguredBatchSize()`
- ✅ `ShouldUseConfiguredTimeout()`
- ✅ `ShouldUseConfiguredNamingStrategy()`

#### 2.2 AgentDiscoveryService
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/AgentDiscoveryServiceTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateServiceWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenConfigurationIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenLoggerIsNull()`

**Discovery Tests:**
- ✅ `ShouldDiscoverEligibleAgentTypes()`
- ✅ `ShouldFilterByBaseType()`
- ✅ `ShouldFilterByRequiredAttributes()`
- ✅ `ShouldFilterByStorageProvider()`
- ✅ `ShouldExcludeConfiguredTypes()`
- ✅ `ShouldIncludeOnlyConfiguredAssemblies()`

**Type Analysis Tests:**
- ✅ `ShouldDetectGuidKeyAgents()`
- ✅ `ShouldDetectStringKeyAgents()`
- ✅ `ShouldDetectLongKeyAgents()`
- ✅ `ShouldDetectIntKeyAgents()`
- ✅ `ShouldDetectCompoundKeyAgents()`

**Eligibility Tests:**
- ✅ `ShouldReturnTrue_ForEligibleAgent()`
- ✅ `ShouldReturnFalse_ForIneligibleAgent()`
- ✅ `ShouldReturnFalse_ForAbstractTypes()`
- ✅ `ShouldReturnFalse_ForInterfaceTypes()`

**Caching Tests:**
- ✅ `ShouldCacheDiscoveredTypes()`
- ✅ `ShouldReturnCachedResults_OnSubsequentCalls()`
- ✅ `ShouldRespectCacheConfiguration()`

**Configuration Tests:**
- ✅ `ShouldUseConfiguredBaseTypes()`
- ✅ `ShouldUseConfiguredAttributes()`
- ✅ `ShouldUseConfiguredExclusions()`
- ✅ `ShouldUseConfiguredAssemblies()`

**Error Handling Tests:**
- ✅ `ShouldHandleAssemblyLoadFailures()`
- ✅ `ShouldHandleTypeAnalysisErrors()`
- ✅ `ShouldLogDiscoveryErrors()`

#### 2.3 AgentWarmupOrchestrator<TIdentifier>
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/AgentWarmupOrchestratorTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateOrchestratorWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenAgentFactoryIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenConfigurationIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenLoggerIsNull()`

**Execution Plan Tests:**
- ✅ `ShouldCreateExecutionPlan_WithStrategies()`
- ✅ `ShouldOrderStrategiesByPriority()`
- ✅ `ShouldAssignAgentTypesToStrategies()`
- ✅ `ShouldHandleUnassignedAgentTypes()`
- ✅ `ShouldHandleEmptyStrategiesList()`
- ✅ `ShouldHandleEmptyAgentTypesList()`

**Strategy Assignment Tests:**
- ✅ `ShouldAssignSpecificStrategiesFirst()`
- ✅ `ShouldAssignDefaultStrategyToRemaining()`
- ✅ `ShouldHandleMultipleApplicableStrategies()`
- ✅ `ShouldRespectStrategyPriority()`

**Execution Tests:**
- ✅ `ShouldExecuteStrategiesInOrder()`
- ✅ `ShouldWarmupAgentsFromStrategies()`
- ✅ `ShouldRespectConcurrencyLimits()`
- ✅ `ShouldRespectRateLimits()`
- ✅ `ShouldHandleCancellation()`

**Agent Activation Tests:**
- ✅ `ShouldActivateAgentsCorrectly()`
- ✅ `ShouldHandleActivationFailures()`
- ✅ `ShouldTrackActivationProgress()`
- ✅ `ShouldLogActivationResults()`

**Error Handling Tests:**
- ✅ `ShouldHandleStrategyFailures()`
- ✅ `ShouldContinueAfterIndividualFailures()`
- ✅ `ShouldLogExecutionErrors()`

#### 2.4 AgentWarmupService<TIdentifier>
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/AgentWarmupServiceTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateServiceWithValidParameters()`
- ✅ `ShouldThrowArgumentNullException_WhenOrchestratorIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenDiscoveryServiceIsNull()`
- ✅ `ShouldThrowArgumentNullException_WhenConfigurationIsNull()`

**Hosted Service Tests:**
- ✅ `ShouldImplementIHostedService()`
- ✅ `ShouldStartWarmupOnStartAsync()`
- ✅ `ShouldStopWarmupOnStopAsync()`
- ✅ `ShouldHandleStartupFailures()`
- ✅ `ShouldHandleShutdownGracefully()`

**Warmup Execution Tests:**
- ✅ `ShouldExecuteWarmupWhenEnabled()`
- ✅ `ShouldSkipWarmupWhenDisabled()`
- ✅ `ShouldDiscoverAgentTypes()`
- ✅ `ShouldCreateExecutionPlan()`
- ✅ `ShouldExecutePlan()`

**Configuration Tests:**
- ✅ `ShouldRespectEnabledConfiguration()`
- ✅ `ShouldRespectConcurrencyConfiguration()`
- ✅ `ShouldRespectRateLimitConfiguration()`

**Progress Tracking Tests:**
- ✅ `ShouldTrackWarmupProgress()`
- ✅ `ShouldReportWarmupResults()`
- ✅ `ShouldLogWarmupStatistics()`

**Error Handling Tests:**
- ✅ `ShouldHandleWarmupFailures()`
- ✅ `ShouldLogWarmupErrors()`
- ✅ `ShouldContinueAfterErrors()`

### 3. Configuration Classes

#### 3.1 AgentWarmupConfiguration
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/AgentWarmupConfigurationTests.cs`

**Default Values Tests:**
- ✅ `ShouldHaveCorrectDefaultValues()`
- ✅ `ShouldInitializeSubConfigurations()`

**Validation Tests:**
- ✅ `ShouldValidateMaxConcurrency()`
- ✅ `ShouldValidateRateLimit()`
- ✅ `ShouldValidateTimeout()`

**Serialization Tests:**
- ✅ `ShouldSerializeToJson()`
- ✅ `ShouldDeserializeFromJson()`
- ✅ `ShouldHandlePartialConfiguration()`

#### 3.2 AutoDiscoveryConfiguration
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/AutoDiscoveryConfigurationTests.cs`

**Default Values Tests:**
- ✅ `ShouldHaveCorrectDefaultValues()`
- ✅ `ShouldInitializeCollections()`

**Validation Tests:**
- ✅ `ShouldValidateBaseTypes()`
- ✅ `ShouldValidateRequiredAttributes()`
- ✅ `ShouldValidateAssemblyNames()`

#### 3.3 MongoDbIntegrationConfiguration
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/MongoDbIntegrationConfigurationTests.cs`

**Default Values Tests:**
- ✅ `ShouldHaveCorrectDefaultValues()`

**Validation Tests:**
- ✅ `ShouldValidateBatchSize()`
- ✅ `ShouldValidateTimeout()`
- ✅ `ShouldValidateNamingStrategy()`

### 4. Extension Methods

#### 4.1 AgentWarmupExtensions
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/AgentWarmupExtensionsTests.cs`

**Service Registration Tests:**
- ✅ `ShouldRegisterAgentWarmupServices_ForGuid()`
- ✅ `ShouldRegisterAgentWarmupServices_ForString()`
- ✅ `ShouldRegisterAgentWarmupServices_ForLong()`
- ✅ `ShouldRegisterAgentWarmupServices_ForInt()`

**Configuration Tests:**
- ✅ `ShouldApplyConfiguration_WhenProvided()`
- ✅ `ShouldUseDefaults_WhenNoConfiguration()`

**Strategy Registration Tests:**
- ✅ `ShouldRegisterSampleBasedStrategy()`
- ✅ `ShouldRegisterPredefinedStrategy()`
- ✅ `ShouldRegisterCustomStrategy()`

**Base Type Configuration Tests:**
- ✅ `ShouldConfigureWithBaseType()`
- ✅ `ShouldConfigureWithMultipleBaseTypes()`

**Orleans Integration Tests:**
- ✅ `ShouldRegisterWithSiloBuilder()`
- ✅ `ShouldConfigureSiloServices()`

**Error Handling Tests:**
- ✅ `ShouldThrowException_WhenServicesIsNull()`
- ✅ `ShouldThrowException_WhenBuilderIsNull()`

### 5. Execution Plan Classes

#### 5.1 WarmupExecutionPlan
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/WarmupExecutionPlanTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateEmptyPlan()`
- ✅ `ShouldInitializeCollections()`

**Strategy Management Tests:**
- ✅ `ShouldAddStrategyExecution()`
- ✅ `ShouldRemoveStrategyExecution()`
- ✅ `ShouldOrderByPriority()`

**Agent Type Management Tests:**
- ✅ `ShouldTrackUnassignedAgentTypes()`
- ✅ `ShouldMoveAssignedTypes()`

#### 5.2 StrategyExecution
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/StrategyExecutionTests.cs`

**Constructor Tests:**
- ✅ `ShouldCreateWithStrategy()`
- ✅ `ShouldInitializeTargetTypes()`

**Property Tests:**
- ✅ `ShouldSetPriority()`
- ✅ `ShouldManageTargetTypes()`

### 6. Utility Classes

#### 6.1 DiagnosticServiceProvider
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/DiagnosticServiceProviderTests.cs`

**Service Resolution Tests:**
- ✅ `ShouldResolveRegisteredServices()`
- ✅ `ShouldThrowException_ForUnregisteredServices()`

**Diagnostic Tests:**
- ✅ `ShouldProvideServiceDiagnostics()`
- ✅ `ShouldTrackServiceResolution()`

### 7. Integration Test Enhancements

#### 7.1 End-to-End Scenarios
**File**: `test/Aevatar.Silo.Tests/AgentWarmup/Tests/EndToEndWarmupTests.cs`

**Complete Workflow Tests:**
- ✅ `ShouldExecuteCompleteWarmupWorkflow()`
- ✅ `ShouldWarmupWithSampleBasedStrategy()`
- ✅ `ShouldWarmupWithPredefinedStrategy()`
- ✅ `ShouldWarmupWithMixedStrategies()`

**Error Recovery Tests:**
- ✅ `ShouldRecoverFromMongoDBFailures()`
- ✅ `ShouldRecoverFromAgentActivationFailures()`
- ✅ `ShouldContinueAfterPartialFailures()`

**Functional Behavior Tests:**
- ✅ `ShouldRespectConcurrencyLimitsCorrectly()`
- ✅ `ShouldHandleReasonableAgentCounts()`
- ✅ `ShouldMaintainCorrectExecutionOrder()`

> **Note**: Performance tests (benchmarks, load testing, throughput measurements) should be implemented in a separate performance test project, not as part of unit tests. Unit tests focus on correctness, not performance metrics.

## Test Implementation Priority

### Phase 1: Core Strategy Tests (High Priority)
1. `SampleBasedAgentWarmupStrategy<TIdentifier>` - **NEW STRATEGY**
2. `DefaultAgentWarmupStrategy<TIdentifier>`
3. `PredefinedAgentWarmupStrategy<TIdentifier>`
4. `BaseAgentWarmupStrategy<TIdentifier>`

### Phase 2: Service Layer Tests (High Priority)
1. `MongoDbAgentIdentifierService`
2. `AgentDiscoveryService`
3. `AgentWarmupOrchestrator<TIdentifier>`

### Phase 3: Configuration and Extensions (Medium Priority)
1. Configuration classes
2. Extension methods
3. Execution plan classes

### Phase 4: Integration and Performance (Medium Priority)
1. End-to-end scenarios
2. Performance tests
3. Error recovery tests

## Test Infrastructure Requirements

### Test Fixtures Needed:
- ✅ `AgentWarmupTestFixture` (exists)
- ❌ `MongoDbTestFixture` (for MongoDB integration tests)
- ❌ `StrategyTestFixture` (for strategy testing)
- ❌ `ConfigurationTestFixture` (for configuration testing)

### Mock Objects Needed:
- ❌ `MockMongoDbAgentIdentifierService`
- ❌ `MockAgentDiscoveryService`
- ❌ `MockAgentWarmupOrchestrator`
- ❌ `MockGrainFactory`

### Test Data Providers:
- ❌ `TestAgentIdentifierProvider`
- ❌ `TestConfigurationProvider`
- ❌ `TestAgentTypeProvider`

## Success Criteria

### Coverage Targets:
- **Line Coverage**: 90%+
- **Branch Coverage**: 85%+
- **Method Coverage**: 95%+

### Quality Metrics:
- All tests must pass consistently
- Tests must be fast and deterministic (unit tests should complete in milliseconds)
- No flaky tests allowed
- All edge cases covered
- Comprehensive error handling validation

### Documentation:
- Each test class must have clear documentation
- Test methods must have descriptive names
- Complex test scenarios must have inline comments
- Test data must be well-organized and reusable

## Notes

- **MongoDB Testing**: Use in-memory MongoDB or test containers for isolation
- **Async Testing**: All async methods must be properly tested with cancellation tokens
- **Performance Testing**: Should be implemented in separate performance test projects, not unit tests
- **Error Simulation**: Test all error scenarios including network failures, timeouts, and invalid data
- **Configuration Testing**: Test all configuration combinations and edge cases
- **Thread Safety**: Test concurrent access scenarios where applicable

---

**Last Updated**: June 9, 2025  
**Status**: Planning Phase  
**Next Action**: Implement Phase 1 strategy tests 