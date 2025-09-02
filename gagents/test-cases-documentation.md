# AIGAgent Unit Test Cases Documentation

This document provides a comprehensive overview of all unit test cases implemented in the `Aevatar.GAgents.AIGAgent.Test` project, organized by test file and functionality.

## 📋 Test Summary

- **Total Test Files**: 10
- **Total Test Cases**: 44
- **Test Categories**: Core functionality, AI integration, LLM configuration, Mock implementations

---

## 🧪 Test Files and Cases

### 1. AIGAgentBaseUnitTest.cs
**Purpose**: Tests centralized LLM configuration functionality and state transition logic in AIGAgentBase

| Test Case | Description |
|-----------|-------------|
| `GetLLMConfigAsync_Should_ReturnSystemConfig_When_LLMConfigKeyIsSet` | Verifies that when an LLMConfigKey is set, the system returns the corresponding centralized configuration from appsettings.json |
| `GetLLMConfigAsync_Should_FallbackToSystemLLM_When_LLMConfigKeyIsNull` | Tests fallback behavior to SystemLLM configuration when LLMConfigKey is null |
| `GetLLMConfigAsync_Should_FallbackToResolvedLLM_When_BothKeysAreNull` | Tests fallback to self-provided LLM configuration when both LLMConfigKey and SystemLLM are null |
| `GetLLMConfigAsync_Should_ReturnNull_When_SystemConfigNotFound` | Verifies that null is returned when referencing a non-existent system configuration |
| `GetLLMConfigAsync_Should_UsePriorityOrder_When_MultipleConfigsSet` | Tests the priority order: LLMConfigKey (highest) > SystemLLM > self-provided LLM (lowest) |

### 2. AIGAgentWithMocksTest.cs
**Purpose**: Demonstrates using mock LLM services with actual AIGAgent tests for reliable unit testing

| Test Case | Description |
|-----------|-------------|
| `Should_UseMockBrainFactory_When_TestModuleConfigured` | Verifies that the test module correctly injects MockBrainFactory instead of real brain factory |
| `Should_InitializeSuccessfully_When_UsingMockBrain` | Tests successful agent initialization using mock brain implementations |
| `Should_ReturnMockResponse_When_PromptChatAsync` | Verifies that chat prompts return configured mock responses instead of calling real AI services |
| `Should_ConfigureMockResponse_When_CustomResponseSet` | Tests the ability to configure custom mock responses for specific test scenarios |
| `Should_HandleMultipleProviders_When_DifferentConfigurations` | Verifies that different LLM provider configurations (OpenAI, Azure, Google) work correctly with mocks |

### 3. AIHttpAsyncChatTest.cs
**Purpose**: Tests HTTP-based asynchronous chat functionality

| Test Case | Description |
|-----------|-------------|
| `AIHttpChatAsyncTest` | Tests asynchronous HTTP chat functionality, verifying that the chat agent can process prompts and update its state with responses over time |

### 4. AIStreamChatTest.cs
**Purpose**: Tests streaming chat functionality and chat with history

| Test Case | Description |
|-----------|-------------|
| `AIChatStreamAsyncTest` | Tests streaming chat functionality, verifying that the agent can handle streaming responses and update its content list |
| `ChatWithHistoryAsyncTest` | Tests chat with history functionality, verifying that the ChatWithHistoryGAgent maintains conversation history correctly |

### 5. AITextToImage.cs
**Purpose**: Tests AI text-to-image functionality with different response types

| Test Case | Description |
|-----------|-------------|
| `GenerateContentTest` | Tests text-to-image generation with Base64Content response type, verifying that the agent returns valid image data |
| `GenerateImageUrlTest` | Tests text-to-image generation with URL response type, verifying that the agent returns image URLs instead of Base64 content |
| `TextToImageAsyncTest` | Tests asynchronous text-to-image processing, verifying that the agent updates its state with generated images over time |

### 6. LLMConfigurationMigrationTest.cs
**Purpose**: Tests automatic migration logic for LLM configuration centralization from legacy to new format

| Test Case | Description |
|-----------|-------------|
| `OnGAgentActivateAsync_Should_MigrateSystemLLMToLLMConfigKey_When_LegacyFormatDetected` | Tests migration from legacy SystemLLM to new LLMConfigKey format during grain activation |
| `OnGAgentActivateAsync_Should_MigrateResolvedLLMToSystemLLM_When_LegacyResolvedConfigExists` | Tests that self-provided LLM configurations are not migrated (remain as-is) |
| `OnGAgentActivateAsync_Should_NotMigrate_When_AlreadyUsingNewFormat` | Verifies that agents already using the new centralized format are not affected by migration |
| `OnGAgentActivateAsync_Should_NotMigrate_When_NoConfigurationExists` | Tests that agents with no configuration are not affected by migration logic |
| `OnGAgentActivateAsync_Should_PreserveBothConfigs_When_MigrationOccurs` | Tests migration behavior when both legacy SystemLLM and resolved LLM exist |
| `OnGAgentActivateAsync_Should_HandleInvalidSystemLLMDuringMigration` | Tests graceful handling of invalid SystemLLM keys during migration |
| `OnGAgentActivateAsync_Should_AutoMigrateOnStartup_When_ExistingAgentHasOnlySystemLLM` | Tests automatic migration on grain activation when existing agent has SystemLLM from appsettings but no LLMConfigKey (simulates real-world upgrade scenario) |

### 7. LLMConfigurationCentralizationTest.cs
**Purpose**: Tests the LLM configuration centralization feature and backward compatibility

| Test Case | Description |
|-----------|-------------|
| `Should_NotStoreResolvedConfig_When_SystemLLMIsUsed` | Verifies that centralized configurations don't store resolved config in agent state |
| `Should_StoreSelfLLMConfig_When_SelfLLMConfigIsProvided` | Tests that self-provided LLM configurations are properly stored in agent state |
| `Should_PreserveBackwardCompatibility_WithExistingStateFormat` | Verifies backward compatibility with existing agent state formats |
| `Should_HandleNonExistentSystemLLM_Gracefully` | Tests graceful handling of references to non-existent system configurations |

### 8. MockBrainFactoryTest.cs
**Purpose**: Tests the MockBrainFactory implementation and IBrainFactory contract compliance

| Test Case | Description |
|-----------|-------------|
| `Should_CreateChatBrain_When_GetChatBrain` | Tests creation of MockChatBrain instances via GetChatBrain method |
| `Should_CreateTextToImageBrain_When_GetTextToImageBrain` | Tests creation of MockTextToImageBrain instances for image generation models |
| `Should_CreateBrain_When_CreateBrain` | Tests generic brain creation via CreateBrain method |
| `Should_ReturnSameBrainType_When_CalledMultipleTimes` | Verifies brain instance caching behavior for performance and state sharing |
| `Should_HandleDifferentProviders_When_GetChatBrain` | Tests factory behavior with different LLM providers (OpenAI, Azure, Google, DeepSeek) |
| `Should_HandleTextToImageModels_When_GetTextToImageBrain` | Tests specific handling of text-to-image models in the factory |

### 9. MockChatBrainTest.cs
**Purpose**: Tests the MockChatBrain implementation and IChatBrain contract compliance

| Test Case | Description |
|-----------|-------------|
| `Should_ReturnConfiguredResponse_When_InvokePromptAsync` | Tests configured mock response functionality for prompt invocation |
| `Should_ReturnDefaultResponse_When_NoResponseConfigured` | Verifies default response behavior when no custom response is configured |
| `Should_HandleStreamingRequest_When_InvokePromptStreamingAsync` | Tests streaming response functionality with asynchronous enumerable |
| `Should_StoreInitializationParameters_When_InitializeAsync` | Verifies that initialization parameters are properly stored |
| `Should_SupportKnowledgeUpsert_When_UpsertKnowledgeAsync` | Tests knowledge base upsert functionality (mock implementation) |

### 10. MockTextToImageBrainTest.cs
**Purpose**: Tests the MockTextToImageBrain implementation and ITextToImageBrain contract compliance

| Test Case | Description |
|-----------|-------------|
| `Should_ReturnConfiguredResponse_When_GenerateTextToImageAsync` | Tests configured mock response for text-to-image generation |
| `Should_ReturnDefaultResponse_When_NoResponseConfigured` | Verifies default image generation behavior when no custom response is set |
| `Should_StoreInitializationParameters_When_InitializeAsync` | Tests proper storage of initialization parameters |
| `Should_SupportKnowledgeUpsert_When_UpsertKnowledgeAsync` | Tests knowledge base upsert functionality for text-to-image brain |
| `Should_HandleCancellationToken_When_GenerateTextToImageAsync` | Tests cancellation token support in image generation |
| `Should_RespectResponseType_When_ConfiguredForUrl` | Tests URL response type configuration vs Base64Content |

---

## 🎯 Test Categories

### Core Functionality Tests
- **AIGAgentBaseUnitTest**: Core agent behavior and configuration resolution
- **LLMConfigurationCentralizationTest**: Centralized configuration management
- **LLMConfigurationMigrationTest**: Legacy to new format migration

### AI Integration Tests
- **AIHttpAsyncChatTest**: HTTP-based chat functionality
- **AIStreamChatTest**: Streaming chat and conversation history
- **AITextToImage**: Text-to-image generation capabilities

### Mock Implementation Tests
- **AIGAgentWithMocksTest**: Integration testing with mocks
- **MockBrainFactoryTest**: Mock factory behavior
- **MockChatBrainTest**: Mock chat brain implementation
- **MockTextToImageBrainTest**: Mock image generation brain

---

## 🔧 Test Infrastructure

### Test Base Classes
- **AevatarAIGAgentTestBase**: Provides Orleans TestKit setup and dependency injection
- Uses Orleans clustering with in-memory storage for isolated testing

### Mock Components
- **MockBrainFactory**: Creates mock brain instances without external dependencies
- **MockChatBrain**: Simulates chat AI responses with configurable behavior
- **MockTextToImageBrain**: Simulates image generation with mock data

### Configuration
- Uses `appsettings.json` for centralized LLM configuration testing
- Supports multiple providers: Azure, OpenAI, Google, DeepSeek
- Includes proper Orleans serialization attributes for all DTOs

---

## 📊 Test Coverage Areas

| Area | Coverage |
|------|----------|
| **Agent Initialization** | ✅ Multiple configuration types, error handling |
| **LLM Configuration** | ✅ Centralized configs, migration, priority order |
| **Chat Functionality** | ✅ Async, streaming, with history |
| **Image Generation** | ✅ Base64, URL responses, async processing |
| **Mock Implementations** | ✅ Factory patterns, configurable responses |
| **Orleans Integration** | ✅ Grain lifecycle, event sourcing, serialization |
| **Error Handling** | ✅ Invalid configs, missing dependencies |
| **Backward Compatibility** | ✅ Legacy state migration, format preservation |

---

## ⚠️ Testing Limitations & Considerations

### Orleans TestKit Constraints

**Grain Lifecycle Testing**:
- Orleans TestKit properly triggers standard `OnActivateAsync` methods
- Custom lifecycle methods like `OnGAgentActivateAsync` are called through the inheritance chain
- **State Persistence**: TestKit doesn't persist state between test grain instances
- **Migration Testing**: Uses `TriggerMigrationAsync()` helper method to simulate what happens during real grain activation

**Real vs Test Behavior**:

| Scenario | Production Orleans | Orleans TestKit |
|----------|-------------------|-----------------|
| **Grain Activation** | `OnActivateAsync` → `OnGAgentActivateAsync` automatically | Same behavior ✅ |
| **State Persistence** | State persisted to storage providers | No persistence between instances ❌ |
| **Migration Trigger** | Automatic on reactivation with legacy state | Manual via `TriggerMigrationAsync()` ❌ |
| **Configuration Resolution** | Uses real appsettings.json configs | Same behavior ✅ |

**Migration Test Approach**:
```csharp
// Production: Automatic migration on grain reactivation
// 1. Grain deactivates (timeout/shutdown)
// 2. On next access, new grain instance created
// 3. OnActivateAsync loads persisted state with SystemLLM set
// 4. OnGAgentActivateAsync detects legacy format and migrates automatically

// TestKit: Manual simulation due to no state persistence
var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(agentId);
await agent.SetSystemLLMAsync("OpenAI"); // Set legacy state
await agent.TriggerMigrationAsync();     // Manually simulate what happens on reactivation
```

### Test Reliability Notes

- **Mock Infrastructure**: All AI service calls use mocks for reliable, fast testing
- **Configuration Testing**: Uses real `appsettings.json` to test centralized config resolution
- **Orleans Serialization**: Tests include comprehensive Orleans attribute validation
- **Error Scenarios**: Covers invalid configurations, missing dependencies, and edge cases

---

## 🚀 Running the Tests

```bash
# Run all tests
dotnet test test/Aevatar.GAgents.AIGAgent.Test/

# Run specific test class
dotnet test test/Aevatar.GAgents.AIGAgent.Test/ --filter "FullyQualifiedName~AIGAgentBaseUnitTest"

# Run with detailed output
dotnet test test/Aevatar.GAgents.AIGAgent.Test/ --verbosity normal
```

---

*Generated for branch: `feature/llm-improvements`*  
*Documentation Date: 2025-07-01*