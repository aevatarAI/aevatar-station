# GAgent Unit Testing Guide - Part 1: Test Infrastructure Setup

## Overview

This guide provides comprehensive documentation for setting up test infrastructure when writing unit tests for GAgents on the Aevatar platform. Proper test infrastructure setup is crucial for writing reliable, maintainable, and efficient tests.

## Required Base Classes

### AevatarGAgentTestBase<TStartupModule>

The foundation of all GAgent testing is the `AevatarGAgentTestBase<TStartupModule>` class, which provides:

- Orleans cluster setup and management
- Dependency injection container configuration
- Service resolution capabilities
- Test lifecycle management

```csharp
using Aevatar.GAgents.TestBase;

public abstract class MyGAgentTestBase : AevatarGAgentTestBase<MyTestModule>
    where MyTestModule : IAbpModule
{
    protected readonly IGAgentFactory AgentFactory;
    
    public MyGAgentTestBase()
    {
        AgentFactory = GetRequiredService<IGAgentFactory>();
    }
}
```

### Key Benefits:
- **Automatic Orleans Cluster Setup**: No manual cluster configuration required
- **Service Injection**: Access to all configured services through `GetRequiredService<T>()`
- **Test Isolation**: Each test runs with a clean cluster state
- **Resource Management**: Automatic cleanup of test resources

## Module Configuration

### Test Module Dependencies

Every test project requires a module class that inherits from `AbpModule` and configures the necessary dependencies:

```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    // Add other required modules
)]
public class MyTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure services specific to your test project
        context.Services.AddSingleton<IGAgentFactory>(provider =>
        {
            var clusterClient = provider.GetRequiredService<IClusterClient>();
            return new GAgentFactory(clusterClient);
        });
        
        // Add mock services if needed
        context.Services.AddSingleton<IMyService, MockMyService>();
    }
}
```

### Essential Dependencies:

```csharp
[DependsOn(
    typeof(AevatarGAgentTestBaseModule),    // Base test infrastructure
    typeof(AevatarModule),                   // Core Aevatar functionality
    typeof(AbpAutofacModule),                // Dependency injection
    typeof(AbpTestBaseModule)                // ABP testing framework
)]
```

## Required Using Statements

```csharp
// Core Aevatar and GAgent dependencies
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;

// ABP Framework dependencies
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Testing;

// Testing frameworks
using Xunit;
using Xunit.Abstractions;
using Shouldly;

// Microsoft dependencies
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Orleans dependencies
using Orleans;
using Orleans.TestingHost;
```

## Dependency Injection Patterns

### Service Resolution

Use the `GetRequiredService<T>()` method to resolve dependencies in your test classes:

```csharp
public class MyGAgentTests : MyGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly ILogger<MyGAgentTests> _logger;
    private readonly IClusterClient _clusterClient;
    
    public MyGAgentTests()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _logger = GetRequiredService<ILogger<MyGAgentTests>>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }
}
```

### Common Services to Inject:

```csharp
// Required for GAgent instantiation
IGAgentFactory _agentFactory;

// Required for cluster operations
IClusterClient _clusterClient;
IGrainFactory _grainFactory;

// Required for logging and debugging
ILogger<MyTests> _logger;
ITestOutputHelper _outputHelper;

// Required for configuration
IOptions<MyConfig> _config;
```

## ITestOutputHelper Usage

For better test debugging and logging, inject `ITestOutputHelper`:

```csharp
public class MyGAgentTests : MyGAgentTestBase
{
    private readonly ITestOutputHelper _outputHelper;
    
    public MyGAgentTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    
    [Fact]
    public async Task MyTest()
    {
        _outputHelper.WriteLine("Starting test execution...");
        
        // Your test logic here
        
        _outputHelper.WriteLine("Test completed successfully");
    }
}
```

### Test Class Setup with ITestOutputHelper:

```csharp
public class MyGAgentTests : MyGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly ITestOutputHelper _outputHelper;
    
    public MyGAgentTests(ITestOutputHelper outputHelper)
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _outputHelper = outputHelper;
    }
    
    [Fact]
    public async Task Should_CreateGAgent_When_Requested()
    {
        _outputHelper.WriteLine("Creating GAgent instance...");
        
        var agentId = Guid.NewGuid();
        var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
        
        _outputHelper.WriteLine($"GAgent created with ID: {agentId}");
        
        agent.ShouldNotBeNull();
    }
}
```

## IGAgentFactory Usage Patterns

### Basic GAgent Creation

```csharp
[Fact]
public async Task Should_CreateGAgent_WithSpecificId()
{
    // Arrange
    var agentId = Guid.NewGuid();
    
    // Act
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Assert
    agent.ShouldNotBeNull();
    var retrievedId = agent.GetPrimaryKey();
    retrievedId.ShouldBe(agentId);
}
```

### GAgent Creation with Configuration

```csharp
[Fact]
public async Task Should_CreateGAgent_WithConfiguration()
{
    // Arrange
    var config = new MyGAgentConfig
    {
        Setting1 = "test-value",
        Setting2 = 42
    };
    
    var agentId = Guid.NewGuid();
    
    // Act
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId, config);
    
    // Assert
    agent.ShouldNotBeNull();
    var state = await agent.GetStateAsync();
    state.Setting1.ShouldBe("test-value");
    state.Setting2.ShouldBe(42);
}
```

## Test Attributes and Configuration

### Required Test Class Attributes:

```csharp
public class MyGAgentTests : MyGAgentTestBase
{
    // No special attributes required on the test class
    // Inherit from appropriate test base class
}
```

### Test Method Attributes:

```csharp
[Fact]                           // Single test case
public async Task MySingleTest() { }

[Theory]                         // Test with parameters
[InlineData("test1")]
[InlineData("test2")]
public async Task MyParameterizedTest(string input) { }

[Theory]                         // Test with multiple parameters
[InlineData("input1", 1)]
[InlineData("input2", 2)]
public async Task MyMultiParameterTest(string input, int number) { }

[Trait("Category", "Integration")]  // Test categorization
public async Task MyIntegrationTest() { }
```

## Mock Services Setup

### Mock Service Registration

```csharp
public class MyTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Replace real services with mocks
        context.Services.AddSingleton<IMyExternalService, MockMyExternalService>();
        context.Services.AddSingleton<ILogger<MyTests>>(provider =>
            new TestLogger<MyTests>(provider.GetRequiredService<ITestOutputHelper>()));
    }
}
```

### Mock Service Implementation

```csharp
public class MockMyExternalService : IMyExternalService
{
    private readonly List<string> _calls = new();
    
    public Task<string> GetDataAsync(string input)
    {
        _calls.Add(input);
        return Task.FromResult($"mock-result-for-{input}");
    }
    
    public IReadOnlyList<string> GetCalls() => _calls.AsReadOnly();
}
```

## Configuration Management

### Test Configuration Files

Create `appsettings.json` in your test project:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Aevatar": "Debug"
    }
  },
  "MyConfig": {
    "TestSetting": "test-value"
  }
}
```

### Configuration Access in Tests

```csharp
public class MyGAgentTests : MyGAgentTestBase
{
    private readonly IOptions<MyConfig> _config;
    
    public MyGAgentTests()
    {
        _config = GetRequiredService<IOptions<MyConfig>>();
    }
    
    [Fact]
    public void Should_LoadTestConfiguration()
    {
        var config = _config.Value;
        config.TestSetting.ShouldBe("test-value");
    }
}
```

## Complete Test Class Example

```csharp
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MyProject.Tests;

public class MyGAgentTests : MyGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly ILogger<MyGAgentTests> _logger;
    private readonly ITestOutputHelper _outputHelper;
    private readonly IOptions<MyConfig> _config;
    
    public MyGAgentTests(ITestOutputHelper outputHelper)
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _logger = GetRequiredService<ILogger<MyGAgentTests>>();
        _outputHelper = outputHelper;
        _config = GetRequiredService<IOptions<MyConfig>>();
    }
    
    [Fact]
    public async Task Should_CreateGAgent_When_Requested()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _outputHelper.WriteLine($"Creating GAgent with ID: {agentId}");
        
        // Act
        var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
        
        // Assert
        agent.ShouldNotBeNull();
        var retrievedId = agent.GetPrimaryKey();
        retrievedId.ShouldBe(agentId);
        
        _outputHelper.WriteLine("GAgent created successfully");
    }
    
    [Theory]
    [InlineData("test-input-1")]
    [InlineData("test-input-2")]
    public async Task Should_ProcessInput_When_ValidInput(string input)
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
        
        // Act
        var result = await agent.ProcessInputAsync(input);
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain(input);
        
        _outputHelper.WriteLine($"Processed input: {input} -> {result}");
    }
}
```

## Best Practices

### 1. Test Class Organization
- Inherit from appropriate test base class
- Use constructor injection for dependencies
- Keep test methods focused and single-purpose

### 2. Service Management
- Use `GetRequiredService<T>()` for service resolution
- Register mock services in the test module
- Avoid manual service creation

### 3. Logging and Debugging
- Use `ITestOutputHelper` for test output
- Log important test steps and results
- Include agent IDs and configuration in logs

### 4. Resource Cleanup
- Rely on base class for automatic cleanup
- Avoid manual cluster management
- Use `using` statements for disposable resources

### 5. Configuration
- Use `appsettings.json` for test configuration
- Access configuration through `IOptions<T>`
- Keep test configurations simple and focused

## Troubleshooting

### Common Issues

1. **Missing Dependencies**: Ensure all required modules are listed in `DependsOn`
2. **Service Resolution**: Verify services are registered in the test module
3. **Cluster Issues**: Check that `AevatarGAgentTestBaseModule` is included
4. **Configuration Problems**: Verify `appsettings.json` is copied to output directory

### Debug Tips

- Use `ITestOutputHelper` to log test execution details
- Check service registration in the test module
- Verify module dependencies are correctly configured
- Use debug mode to step through test execution

## Next Steps

After setting up your test infrastructure, proceed to:

- **Part 2: Basic Testing Patterns** - Learn fundamental GAgent testing patterns
- **Part 3: Common Test Scenarios** - Explore specific testing scenarios
- **Part 4: Integration Testing Patterns** - Master advanced testing techniques

## References

- [xUnit Documentation](https://xunit.net/)
- [ABP Framework Testing Documentation](https://docs.abp.io/en/abp/latest/Testing)
- [Orleans Testing Framework](https://docs.microsoft.com/en-us/dotnet/orleans/testing)
- [Shouldly Assertion Library](https://shouldly.readthedocs.io/)