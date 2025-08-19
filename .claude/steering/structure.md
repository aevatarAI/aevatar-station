# Aevatar Station Project Structure & Standards

## Project Structure Overview

### Repository Structure
```
aevatar-station/
├── framework/                    # Orleans-based actor framework
│   ├── src/                      # Framework source code
│   ├── test/                     # Framework tests
│   ├── samples/                  # Framework sample applications
│   └── docs/                     # Framework documentation
├── signalR/                      # SignalR integration layer
│   ├── src/                      # SignalR source code
│   ├── test/                     # SignalR tests
│   └── samples/                  # SignalR sample applications
├── station/                      # Main application platform
│   ├── src/                      # Application source code
│   ├── test/                     # Application tests
│   ├── samples/                  # Application samples
│   ├── benchmark/                # Performance benchmarks
│   └── docs/                     # Application documentation
└── .claude/                      # Claude configuration
    ├── steering/                 # Steering documents (this folder)
    ├── specs/                    # Feature specifications
    └── templates/                # Code templates
```

### Station Application Structure
```
station/src/
├── Aevatar.Domain.Shared/        # Shared domain types and constants
├── Aevatar.Domain/               # Domain entities and business logic
├── Aevatar.Application.Contracts/ # Application DTOs and interfaces
├── Aevatar.Application/          # Application services and workflows
├── Aevator.Application.Grains/   # Orleans grains for business logic
├── Aevatar.HttpApi/              # HTTP API contracts
├── Aevatar.HttpApi.Host/         # Main API host
├── Aevatar.AuthServer/           # Identity and authentication
├── Aevatar.Silo/                 # Orleans silo host
├── Aevatar.Worker/               # Background services
├── Aevatar.CQRS/                 # CQRS implementation
├── Aevatar.MongoDB/              # MongoDB integration
└── Aevatar.DbMigrator/           # Database migrations
```

## Coding Standards

### C# Coding Standards

#### File Organization
```csharp
// File header format (required for new files)
// ABOUTME: This file implements [core functionality]
// ABOUTME: [Brief description of purpose/responsibility]

namespace Aevatar.[Module].[SubModule]
{
    public class ClassName
    {
        // Implementation
    }
}
```

#### Naming Conventions
- **Classes**: PascalCase (e.g., `AgentService`, `UserManagement`)
- **Interfaces**: PascalCase with 'I' prefix (e.g., `IAgentService`, `IUserRepository`)
- **Methods**: PascalCase (e.g., `CreateAgentAsync`, `ValidateUserInput`)
- **Properties**: PascalCase (e.g., `AgentId`, `UserName`, `IsActive`)
- **Fields**: _camelCase with underscore prefix (e.g., `_agentRepository`, `_logger`)
- **Constants**: PascalCase (e.g., `MaxAgentCount`, `DefaultTimeout`)
- **Local Variables**: camelCase (e.g., `agentId`, `userInput`, `isValid`)

#### Agent Development Pattern
```csharp
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class CustomAgent : GAgentBase<CustomAgentState, CustomAgentStateLogEvent>
{
    private readonly ILogger<CustomAgent> _logger;

    public CustomAgent(ILogger<CustomAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Custom Agent Description");
    }

    [EventHandler]
    public async Task HandleCustomEventAsync(CustomEvent @event)
    {
        try
        {
            _logger.LogInformation("Processing custom event: {EventId}", @event.Id);
            
            // Business logic
            State.ProcessEvent(@event);
            
            // Raise state change event
            await RaiseEvent(new CustomAgentStateLogEvent
            {
                Action = "EventProcessed",
                Data = @event,
                Timestamp = DateTime.UtcNow
            });
            
            await ConfirmEvents();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing custom event: {EventId}", @event.Id);
            throw;
        }
    }
}
```

#### Event Handling Standards
```csharp
[EventHandler]
public async Task HandleEventAsync(EventType @event)
{
    // 1. Validate input
    if (@event == null)
    {
        throw new ArgumentNullException(nameof(@event));
    }

    // 2. Log event reception
    _logger.LogInformation("Received event: {EventType}, Id: {EventId}", 
        @event.GetType().Name, @event.Id);

    // 3. Process business logic
    var result = await ProcessEventLogicAsync(@event);

    // 4. Update state
    State.UpdateFromEvent(@event, result);

    // 5. Raise state change event
    await RaiseEvent(new StateChangeEvent { ... });

    // 6. Confirm events
    await ConfirmEvents();
}
```

### Domain-Driven Design Standards

#### Entity Pattern
```csharp
public class Agent : Entity<Guid>, IAggregateRoot
{
    public string Name { get; private set; }
    public string AgentType { get; private set; }
    public AgentStatus Status { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }

    private Agent() { } // For ORM

    public Agent(Guid id, string name, string agentType, Guid organizationId)
    {
        Id = id;
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        AgentType = Guard.Against.NullOrEmpty(agentType, nameof(agentType));
        OrganizationId = organizationId;
        Status = AgentStatus.Active;
        CreatedAt = DateTime.UtcNow;
        
        AddLocalEvent(new AgentCreatedEvent(Id, Name, AgentType));
    }

    public void UpdateName(string newName)
    {
        Name = Guard.Against.NullOrEmpty(newName, nameof(newName));
        LastModifiedAt = DateTime.UtcNow;
        
        AddLocalEvent(new AgentUpdatedEvent(Id, Name));
    }

    public void Deactivate()
    {
        if (Status == AgentStatus.Inactive)
        {
            return;
        }

        Status = AgentStatus.Inactive;
        LastModifiedAt = DateTime.UtcNow;
        
        AddLocalEvent(new AgentDeactivatedEvent(Id));
    }
}
```

#### Value Object Pattern
```csharp
public class AgentConfiguration : ValueObject
{
    public Dictionary<string, object> Settings { get; private set; }
    public TimeSpan Timeout { get; private set; }
    public int MaxRetries { get; private set; }

    private AgentConfiguration() { }

    public AgentConfiguration(Dictionary<string, object> settings, TimeSpan timeout, int maxRetries)
    {
        Settings = settings ?? new Dictionary<string, object>();
        Timeout = timeout;
        MaxRetries = maxRetries > 0 ? maxRetries : 3;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Settings;
        yield return Timeout;
        yield return MaxRetries;
    }
}
```

### Service Layer Standards

#### Application Service Pattern
```csharp
public class AgentAppService : ApplicationService, IAgentAppService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IGAgentFactory _agentFactory;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AgentAppService(
        IAgentRepository agentRepository,
        IGAgentFactory agentFactory,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _agentRepository = agentRepository;
        _agentFactory = agentFactory;
        _unitOfWorkManager = unitOfWorkManager;
    }

    [Authorize]
    public async Task<AgentDto> CreateAsync(CreateAgentDto input)
    {
        using var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions
        {
            RequiresNew = true,
            IsTransactional = true
        });

        try
        {
            // Validate input
            await ValidateCreateAgentAsync(input);

            // Create domain entity
            var agent = new Agent(
                GuidGenerator.Create(),
                input.Name,
                input.AgentType,
                CurrentTenant.Id
            );

            // Save to database
            await _agentRepository.InsertAsync(agent);

            // Create Orleans grain
            var grain = await _agentFactory.GetGAgentAsync<IAgentGrain>(agent.Id);
            await grain.InitializeAsync(new AgentInitializeRequest
            {
                Name = agent.Name,
                Type = agent.AgentType,
                Configuration = input.Configuration
            });

            await uow.CompleteAsync();

            // Return DTO
            return ObjectMapper.Map<Agent, AgentDto>(agent);
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "Error creating agent: {AgentName}", input.Name);
            throw;
        }
    }

    private async Task ValidateCreateAgentAsync(CreateAgentDto input)
    {
        // Business validation logic
        if (await _agentRepository.ExistsAsync(input.Name, CurrentTenant.Id))
        {
            throw new BusinessException(AevatarDomainErrorCodes.AgentNameAlreadyExists)
                .WithData("Name", input.Name);
        }

        // Additional validation rules
        if (string.IsNullOrWhiteSpace(input.AgentType))
        {
            throw new BusinessException(AevatarDomainErrorCodes.AgentTypeRequired);
        }
    }
}
```

### API Standards

#### Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController : AevatarController
{
    private readonly IAgentAppService _agentService;

    public AgentController(IAgentAppService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AgentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgentDto>> CreateAsync([FromBody] CreateAgentDto input)
    {
        var result = await _agentService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AgentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentDto>> GetAsync(Guid id)
    {
        var result = await _agentService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AgentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AgentDto>>> GetListAsync(
        [FromQuery] GetAgentListInput input)
    {
        var result = await _agentService.GetListAsync(input);
        return Ok(result);
    }
}
```

### Testing Standards

#### Unit Testing Pattern
```csharp
public class AgentAppServiceTests : AevatarApplicationTestBase
{
    private readonly IAgentAppService _agentAppService;
    private readonly IAgentRepository _agentRepository;

    public AgentAppServiceTests()
    {
        _agentAppService = GetRequiredService<IAgentAppService>();
        _agentRepository = GetRequiredService<IAgentRepository>();
    }

    [Fact]
    public async Task Should_Create_Agent_Valid_Input()
    {
        // Arrange
        var input = new CreateAgentDto
        {
            Name = "Test Agent",
            AgentType = "ChatAgent",
            Configuration = new Dictionary<string, object>
            {
                ["Model"] = "gpt-4",
                ["Temperature"] = 0.7
            }
        };

        // Act
        var result = await _agentAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(input.Name);
        result.AgentType.ShouldBe(input.AgentType);
        result.Status.ShouldBe(AgentStatus.Active);
    }

    [Fact]
    public async Task Should_Throw_Exception_Duplicate_Name()
    {
        // Arrange
        var existingAgent = await CreateTestAgentAsync("Existing Agent");
        var input = new CreateAgentDto
        {
            Name = "Existing Agent",
            AgentType = "ChatAgent"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(
            () => _agentAppService.CreateAsync(input));
    }

    private async Task<Agent> CreateTestAgentAsync(string name)
    {
        var agent = new Agent(
            GuidGenerator.Create(),
            name,
            "ChatAgent",
            CurrentTenant.Id
        );

        await _agentRepository.InsertAsync(agent);
        return agent;
    }
}
```

#### Grain Testing Pattern
```csharp
public class CustomAgentTests : AevatarOrleansTestBase
{
    [Fact]
    public async Task Should_Handle_Custom_Event()
    {
        // Arrange
        var agent = await GetGrainAsync<ICustomAgent>(Guid.NewGuid());
        var testEvent = new CustomEvent
        {
            Id = Guid.NewGuid(),
            Data = "Test Data",
            Timestamp = DateTime.UtcNow
        };

        // Act
        await agent.HandleCustomEventAsync(testEvent);

        // Assert
        var state = await agent.GetStateAsync();
        state.ProcessedEventsCount.ShouldBe(1);
        state.LastProcessedData.ShouldBe("Test Data");
    }

    [Fact]
    public async Task Should_Publish_Events_On_State_Change()
    {
        // Arrange
        var agent = await GetGrainAsync<ICustomAgent>(Guid.NewGuid());
        var eventCollector = new EventCollector();
        
        // Subscribe to events
        await agent.SubscribeAsync(eventCollector);

        // Act
        await agent.UpdateStateAsync("New State");

        // Assert
        await eventCollector.WaitForEventsAsync(TimeSpan.FromSeconds(5));
        eventCollector.Events.Count.ShouldBeGreaterThan(0);
        eventCollector.Events.ShouldContain(e => e is StateUpdatedEvent);
    }
}
```

### Configuration Standards

#### Configuration Pattern
```csharp
public class AevatarOptions
{
    public const string SectionName = "Aevatar";

    public AgentOptions Agents { get; set; } = new();
    public OrleansOptions Orleans { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
}

public class AgentOptions
{
    public string DefaultAgentType { get; set; } = "DefaultAgent";
    public int MaxAgentsPerOrganization { get; set; } = 1000;
    public TimeSpan AgentTimeoutDuration { get; set; } = TimeSpan.FromMinutes(30);
    public List<string> SupportedAgentTypes { get; set; } = new();
}

// In Program.cs or Startup.cs
builder.Services.Configure<AevatarOptions>(
    builder.Configuration.GetSection(AevatarOptions.SectionName));
```

### Documentation Standards

#### XML Documentation
```csharp
/// <summary>
/// Service for managing AI agents in the Aevatar platform.
/// </summary>
/// <remarks>
/// This service provides CRUD operations for agents, handles agent lifecycle management,
/// and coordinates with Orleans grains for distributed agent execution.
/// </remarks>
public interface IAgentService
{
    /// <summary>
    /// Creates a new agent with the specified configuration.
    /// </summary>
    /// <param name="input">The agent creation parameters.</param>
    /// <returns>The created agent information.</returns>
    /// <exception cref="BusinessException">
    /// Thrown when the agent name already exists or invalid parameters are provided.
    /// </exception>
    Task<AgentDto> CreateAsync(CreateAgentDto input);

    /// <summary>
    /// Retrieves an agent by its unique identifier.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <returns>The agent information, or null if not found.</returns>
    Task<AgentDto?> GetAsync(Guid id);
}
```

### Error Handling Standards

#### Exception Handling Pattern
```csharp
public class AgentService : IAgentService
{
    private readonly ILogger<AgentService> _logger;

    public AgentService(ILogger<AgentService> logger)
    {
        _logger = logger;
    }

    public async Task<AgentDto> CreateAgentAsync(CreateAgentDto input)
    {
        try
        {
            // Validate input
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.Name))
            {
                throw new BusinessException(
                    AevatarDomainErrorCodes.AgentNameRequired);
            }

            // Business logic
            return await CreateAgentInternalAsync(input);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business rule violation creating agent: {AgentName}", 
                input?.Name ?? "null");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating agent: {AgentName}", 
                input?.Name ?? "null");
            throw new BusinessException(
                AevatarDomainErrorCodes.AgentCreationFailed);
        }
    }

    private async Task<AgentDto> CreateAgentInternalAsync(CreateAgentDto input)
    {
        // Implementation logic
        throw new NotImplementedException();
    }
}
```

### Logging Standards

#### Logging Pattern
```csharp
public class AgentEventHandler
{
    private readonly ILogger<AgentEventHandler> _logger;

    public AgentEventHandler(ILogger<AgentEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleEventAsync(AgentEvent @event)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventId"] = @event.Id,
            ["AgentId"] = @event.AgentId,
            ["EventType"] = @event.GetType().Name
        });

        try
        {
            _logger.LogInformation(
                "Processing agent event: {EventType} for agent: {AgentId}",
                @event.GetType().Name, @event.AgentId);

            await ProcessEventInternalAsync(@event);

            _logger.LogInformation(
                "Successfully processed event: {EventId} for agent: {AgentId}",
                @event.Id, @event.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to process event: {EventId} for agent: {AgentId}",
                @event.Id, @event.AgentId);
            throw;
        }
    }
}
```

### Dependency Injection Standards

#### Service Registration Pattern
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAevatarApplication(
        this IServiceCollection services)
    {
        // Register application services
        services.AddTransient<IAgentAppService, AgentAppService>();
        services.AddTransient<IOrganizationAppService, OrganizationAppService>();
        
        // Register domain services
        services.AddTransient<IAgentDomainService, AgentDomainService>();
        
        // Register repositories
        services.AddTransient<IAgentRepository, AgentRepository>();
        
        // Register background services
        services.AddHostedService<AgentMonitoringService>();
        
        return services;
    }
}
```

This structure document provides comprehensive guidance for maintaining consistency across the Aevatar Station codebase while following established patterns and best practices for distributed systems development.