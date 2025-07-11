# TODO-013: Create Dependency Injection Configuration

## Task Overview
Create the dependency injection configuration for all new architecture services, ensuring proper service lifetimes, configuration binding, and integration with the existing ABP Framework and Orleans infrastructure.

## Description
Implement comprehensive dependency injection setup for the new services (AgentLifecycleService, TypeMetadataService, EventPublisher, etc.) while maintaining compatibility with existing ABP modules and Orleans configuration. This includes proper service registration, configuration binding, and health checks.

## Acceptance Criteria
- [ ] Create service registration extensions for new services
- [ ] Configure proper service lifetimes (Singleton, Scoped, Transient)
- [ ] Add configuration binding for service options
- [ ] Integrate with existing ABP Framework modules
- [ ] Configure Orleans-specific dependencies
- [ ] Add Elasticsearch client configuration
- [ ] Create health checks for all services
- [ ] Add service validation and startup checks
- [ ] Create comprehensive unit tests for DI configuration
- [ ] Document service dependencies and lifetimes

## File Locations
- `station/src/Aevatar.Application/Extensions/ServiceCollectionExtensions.cs`
- `station/src/Aevatar.Application/Configuration/AgentArchitectureOptions.cs`
- `station/src/Aevatar.Application/HealthChecks/ServicesHealthCheck.cs`
- `station/src/Aevatar.Application/Modules/AgentArchitectureModule.cs`

## Implementation Details

### Service Registration Extensions
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentArchitecture(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<AgentArchitectureOptions>(
            configuration.GetSection("AgentArchitecture"));
        services.Configure<ElasticsearchOptions>(
            configuration.GetSection("Elasticsearch"));
        services.Configure<EventPublisherOptions>(
            configuration.GetSection("EventPublisher"));
        
        // Core services
        services.AddScoped<IAgentLifecycleService, AgentLifecycleService>();
        services.AddScoped<IAgentDiscoveryService, AgentDiscoveryService>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IAgentFactory, AgentFactory>();
        
        // Singleton services (cached metadata)
        services.AddSingleton<ITypeMetadataService, TypeMetadataService>();
        
        // Infrastructure services
        services.AddElasticsearchClient(configuration);
        services.AddOrleansClient(configuration);
        
        // Health checks
        services.AddAgentArchitectureHealthChecks();
        
        // Validators
        services.AddTransient<IValidator<CreateAgentRequest>, CreateAgentRequestValidator>();
        services.AddTransient<IValidator<UpdateAgentRequest>, UpdateAgentRequestValidator>();
        services.AddTransient<IValidator<AgentDiscoveryQuery>, AgentDiscoveryQueryValidator>();
        
        return services;
    }
    
    private static IServiceCollection AddElasticsearchClient(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddSingleton<IElasticsearchClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var settings = new ElasticsearchClientSettings(new Uri(options.ConnectionString))
                .DefaultIndex(options.DefaultIndex)
                .RequestTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds))
                .EnableDebugMode(options.EnableDebugMode);
            
            if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
            {
                settings.Authentication(new BasicAuthentication(options.Username, options.Password));
            }
            
            return new ElasticsearchClient(settings);
        });
        
        return services;
    }
    
    private static IServiceCollection AddOrleansClient(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddSingleton<IClusterClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OrleansOptions>>().Value;
            
            var clientBuilder = new ClientBuilder()
                .UseLocalhostClustering()
                .ConfigureLogging(logging => logging.AddConsole());
            
            // Configure based on environment
            if (options.Environment == "Development")
            {
                clientBuilder.UseLocalhostClustering();
            }
            else
            {
                clientBuilder.UseAdoNetClustering(options => 
                {
                    options.ConnectionString = configuration.GetConnectionString("Orleans");
                    options.Invariant = "System.Data.SqlClient";
                });
            }
            
            return clientBuilder.Build();
        });
        
        return services;
    }
    
    private static IServiceCollection AddAgentArchitectureHealthChecks(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<TypeMetadataServiceHealthCheck>("type-metadata-service")
            .AddCheck<ElasticsearchHealthCheck>("elasticsearch")
            .AddCheck<OrleansHealthCheck>("orleans-cluster")
            .AddCheck<EventPublisherHealthCheck>("event-publisher");
        
        return services;
    }
}
```

### Configuration Options Classes
```csharp
public class AgentArchitectureOptions
{
    public const string SectionName = "AgentArchitecture";
    
    public bool EnableNewArchitecture { get; set; } = true;
    public bool EnableLegacySupport { get; set; } = false;
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 1000;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnablePerformanceMonitoring { get; set; } = true;
}

public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";
    
    public string ConnectionString { get; set; } = "http://localhost:9200";
    public string DefaultIndex { get; set; } = "agent-instances";
    public string Username { get; set; }
    public string Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableDebugMode { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public bool EnableSniffing { get; set; } = true;
}

public class EventPublisherOptions
{
    public const string SectionName = "EventPublisher";
    
    public string DefaultStreamProvider { get; set; } = "Aevatar";
    public int RetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public int BatchSize { get; set; } = 100;
    public bool EnableDeadLetterQueue { get; set; } = true;
    public TimeSpan StreamHealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}

public class OrleansOptions
{
    public const string SectionName = "Orleans";
    
    public string Environment { get; set; } = "Development";
    public string ClusterId { get; set; } = "aevatar-cluster";
    public string ServiceId { get; set; } = "aevatar-service";
    public string ConnectionString { get; set; }
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### ABP Framework Module
```csharp
[DependsOn(
    typeof(AevatarApplicationContractsModule),
    typeof(AevatarDomainModule),
    typeof(AbpAutoMapperModule)
)]
public class AgentArchitectureModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // Pre-configure Orleans client
        context.Services.PreConfigure<OrleansOptions>(options =>
        {
            configuration.GetSection(OrleansOptions.SectionName).Bind(options);
        });
    }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // Add agent architecture services
        context.Services.AddAgentArchitecture(configuration);
        
        // Configure AutoMapper
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AgentArchitectureModule>();
        });
        
        // Configure background services
        context.Services.AddHostedService<TypeMetadataInitializationService>();
        context.Services.AddHostedService<ElasticsearchIndexInitializationService>();
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();
        
        // Initialize services
        var serviceProvider = context.ServiceProvider;
        
        // Ensure Orleans client is connected
        var orleansClient = serviceProvider.GetRequiredService<IClusterClient>();
        orleansClient.Connect().Wait();
        
        // Initialize Elasticsearch indices
        var indexManager = serviceProvider.GetRequiredService<AgentInstanceIndexManager>();
        indexManager.EnsureIndexExistsAsync().Wait();
        
        // Health checks endpoint
        if (env.IsDevelopment())
        {
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }
    }
}
```

### Health Checks Implementation
```csharp
public class TypeMetadataServiceHealthCheck : IHealthCheck
{
    private readonly ITypeMetadataService _typeMetadataService;
    
    public TypeMetadataServiceHealthCheck(ITypeMetadataService typeMetadataService)
    {
        _typeMetadataService = typeMetadataService;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var types = await _typeMetadataService.GetAllTypesAsync();
            
            if (types.Count == 0)
            {
                return HealthCheckResult.Degraded("No agent types found in metadata service");
            }
            
            return HealthCheckResult.Healthy($"Found {types.Count} agent types");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Type metadata service is not responding", ex);
        }
    }
}

public class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly IElasticsearchClient _client;
    
    public ElasticsearchHealthCheck(IElasticsearchClient client)
    {
        _client = client;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.PingAsync(cancellationToken: cancellationToken);
            
            if (response.IsValid)
            {
                return HealthCheckResult.Healthy("Elasticsearch is responding");
            }
            
            return HealthCheckResult.Unhealthy($"Elasticsearch ping failed: {response.DebugInformation}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to Elasticsearch", ex);
        }
    }
}

public class OrleansHealthCheck : IHealthCheck
{
    private readonly IClusterClient _clusterClient;
    
    public OrleansHealthCheck(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_clusterClient.IsInitialized)
            {
                return HealthCheckResult.Unhealthy("Orleans client is not initialized");
            }
            
            // Test with a simple grain call
            var managementGrain = _clusterClient.GetGrain<IManagementGrain>(0);
            var hosts = await managementGrain.GetHosts();
            
            return HealthCheckResult.Healthy($"Orleans cluster has {hosts.Count} active silos");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Orleans cluster is not responding", ex);
        }
    }
}
```

### Service Validators
```csharp
public class CreateAgentRequestValidator : AbstractValidator<CreateAgentRequest>
{
    public CreateAgentRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
        
        RuleFor(x => x.AgentType)
            .NotEmpty()
            .WithMessage("AgentType is required")
            .MaximumLength(100)
            .WithMessage("AgentType must not exceed 100 characters");
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");
        
        RuleFor(x => x.Properties)
            .NotNull()
            .WithMessage("Properties cannot be null");
    }
}

public class AgentDiscoveryQueryValidator : AbstractValidator<AgentDiscoveryQuery>
{
    public AgentDiscoveryQueryValidator()
    {
        RuleFor(x => x.Take)
            .GreaterThan(0)
            .WithMessage("Take must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Take must not exceed 1000");
        
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be greater than or equal to 0");
    }
}
```

## Configuration Files

### appsettings.json
```json
{
  "AgentArchitecture": {
    "EnableNewArchitecture": true,
    "EnableLegacySupport": false,
    "DefaultPageSize": 50,
    "MaxPageSize": 1000,
    "CacheExpiration": "00:30:00",
    "EnablePerformanceMonitoring": true
  },
  "Elasticsearch": {
    "ConnectionString": "http://localhost:9200",
    "DefaultIndex": "agent-instances",
    "TimeoutSeconds": 30,
    "EnableDebugMode": false,
    "MaxRetries": 3,
    "EnableSniffing": true
  },
  "EventPublisher": {
    "DefaultStreamProvider": "Aevatar",
    "RetryAttempts": 3,
    "RetryDelay": "00:00:00.100",
    "BatchSize": 100,
    "EnableDeadLetterQueue": true,
    "StreamHealthCheckInterval": "00:01:00"
  },
  "Orleans": {
    "Environment": "Development",
    "ClusterId": "aevatar-cluster",
    "ServiceId": "aevatar-service",
    "ResponseTimeout": "00:00:30"
  }
}
```

## Dependencies
- All new service interfaces and implementations (TODO-002 through TODO-008)
- ABP Framework modules
- Orleans client libraries
- Elasticsearch client libraries
- FluentValidation
- Health check libraries

## Testing Requirements
- Unit tests for service registration
- Integration tests for DI container resolution
- Health check functionality tests
- Configuration binding tests
- Service lifetime validation tests
- Startup sequence tests
- Error handling during initialization tests

## Integration Points
- ABP Framework module system
- Existing Orleans silo configuration
- Current Elasticsearch setup
- Existing health check infrastructure
- Monitoring and logging systems

## Performance Considerations
- Singleton vs Scoped service lifetime decisions
- Lazy initialization where appropriate
- Connection pooling for external services
- Health check execution frequency
- Configuration caching strategies

## Security Considerations
- Secure configuration of credentials
- Encrypted connections to external services
- Proper service isolation
- Audit logging for service access
- Rate limiting for expensive operations

## Error Handling Strategy
- Graceful degradation when services unavailable
- Circuit breaker patterns for external dependencies
- Comprehensive error logging
- Health check failure notifications
- Service initialization retry logic

## Success Metrics
- All services resolve correctly from DI container
- Health checks pass for all components
- Zero service initialization failures
- Proper service lifetime management
- Configuration validation works correctly

## Migration Strategy
- Implement alongside existing DI configuration
- Use feature flags to enable/disable new services
- Gradual replacement of existing service registrations
- Monitor for DI container performance impact
- Plan rollback for initialization failures

## Priority: Medium
This should be implemented after the core services are complete and before integration testing begins.