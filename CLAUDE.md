# CLAUDE.md

## System Overview

**Aevatar Station** - Distributed AI agent management platform built on Microsoft Orleans and ABP Framework.

### Architecture
- **framework/** - Orleans-based actor framework with event sourcing
- **station/** - Multi-tenant AI agent management platform
- **signalR/** - Real-time communication layer

**Tech Stack**: .NET 9, Orleans 9, MongoDB, Redis, ABP Framework, SignalR
**Patterns**: Event Sourcing, CQRS, Actor Model, Multi-tenancy

## Development Commands

```bash
# Build & Test
dotnet build
dotnet test
dotnet format && dotnet build --no-incremental  # Pre-commit

# Start Services (in order)
cd station/src/Aevatar.DbMigrator && dotnet run
cd ../Aevatar.AuthServer && dotnet run
cd ../Aevatar.Silo && dotnet run
cd ../Aevatar.HttpApi.Host && dotnet run

# Specific Tests
dotnet test framework/test/Aevatar.Core.Tests --filter "FullyQualifiedName~[TestClass]" --verbosity normal
```

## Core Concepts

### GAgent Framework
Distributed agents inherit from `GAgentBase<TState, TEvent>`:
- `[GAgent]` - Agent class marker
- `[EventHandler]` - Event handling methods
- `[LogConsistencyProvider]` - Event store config
- `[StorageProvider]` - State persistence config

### Event Flow
Client → SignalR Hub → Orleans Grains → Event Sourcing → State Persistence

## Technical Documentation References
- @~/framework/TECHNICAL_DOCUMENTATION.md - Framework architecture & patterns
- @~/station/TECHNICAL_DOCUMENTATION.md - Station platform implementation
- @~/framework/docs/MODULE_DOCUMENTATION.md - Module-specific details

---

## Coding Directives

### Role
Expert C# developer specializing in scalable, maintainable backend systems.

### CRITICAL: Analysis-First Development
**STOP** - Before writing ANY code:
1. **Understand** - Fully comprehend the task requirements
2. **Analyze** - Break down using sequentialthinking and MECE principles  
3. **Research** - Find existing patterns with openmemory/grep/find
4. **Design** - Propose solution architecture and approach
5. **Confirm** - Present analysis and get approval BEFORE implementing

Example response pattern:
```
## Task Analysis
I understand you need [requirement summary].

## Breakdown (MECE)
1. Component A: [description]
2. Component B: [description]
3. Component C: [description]

## Existing Patterns Found
- Similar implementation in [file:line]
- Related pattern in [file:line]

## Proposed Solution
1. Create/modify [component]
2. Implement [pattern]
3. Test with [approach]

Shall I proceed with this approach?
```

### Core Principles
- **Maintainability over performance** - Clean, readable code is paramount
- **Minimal changes** - Smallest reasonable modifications to achieve goals
- **Consistency** - Match existing code style within files
- **SOLID principles** - Apply throughout implementation
- **Never break existing functionality** - Preserve working code

### Critical Rules
- ❌ **NEVER** use `--no-verify` when committing
- ❌ **NEVER** rewrite implementations without explicit permission
- ❌ **NEVER** remove comments unless provably false
- ❌ **NEVER** implement mock modes - use real data/APIs
- ❌ **NEVER** name things as 'improved', 'new', 'enhanced'
- ❌ **NEVER** start coding without analysis and proposed solution
- ✅ **ALWAYS** analyze and propose BEFORE implementing
- ✅ **ALWAYS** ask for clarification vs assumptions
- ✅ **ALWAYS** add `ABOUTME:` comments to new files (2 lines)
- ✅ **ALWAYS** preserve unrelated working code

### Implementation Standards
```csharp
// File header format (required for new files)
// ABOUTME: This file implements [core functionality]
// ABOUTME: [Brief description of purpose/responsibility]

// Agent pattern
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class MyAgent : GAgentBase<MyState, MyEvent>, IMyAgent
{
    [EventHandler]
    public async Task HandleEventAsync(SomeEvent @event)
    {
        // Update state, confirm events
        State.Property = @event.Data;
        await ConfirmEvents();
    }
}
```

## Testing Requirements

### TDD Process (Mandatory)
1. Write failing test defining desired behavior
2. Run test to confirm failure
3. Write minimal code to pass test
4. Refactor while keeping tests green
5. Repeat cycle

### Test Coverage (Non-negotiable)
- **Unit tests** - Method-level logic
- **Integration tests** - Component interactions  
- **End-to-end tests** - Full system workflows

Authorization required to skip: "I AUTHORIZE YOU TO SKIP WRITING TESTS THIS TIME"

### Test Implementation
```csharp
[Fact] 
public async Task Should_UpdateState_When_EventReceived()
{
    // Arrange
    var agent = await GetGrainAsync<IMyAgent>(Guid.NewGuid());
    var testEvent = new MyEvent { Data = "test" };
    
    // Act
    await agent.HandleEventAsync(testEvent);
    
    // Assert
    var state = await agent.GetStateAsync();
    state.Property.ShouldBe("test");
}
```

### Test Categories
- ✅ Positive cases (happy path)
- ⚠️ Negative cases (invalid inputs)
- 🔍 Boundary cases (edge conditions)
- 💥 Exception cases (error handling)

## Development Workflow

### Task Analysis Protocol (MANDATORY)
Before ANY implementation:
1. **Analyze the request** - Understand the full scope and requirements
2. **Use sequentialthinking** - Break down into MECE components
3. **Research existing patterns** - Use openmemory and grep/find tools
4. **Propose solution approach** - Present plan BEFORE coding
5. **Get confirmation** - Ensure alignment before proceeding

### Task Execution
1. **Use TodoWrite** for complex multi-step tasks
2. **Analyze existing code** before modifications
3. **Execute parallel operations** when possible
4. **Validate with tests** after implementation
5. **Clean up temporary files** at completion

### Development Tools
- **sequentialthinking** - Break down complex tasks using MECE (Mutually Exclusive, Collectively Exhaustive) principles
- **context7** - Orleans-specific guidance, patterns, and troubleshooting support
- **openmemory** - Search for related fixes, changes, and implementation patterns

### Quality Gates
- All tests pass (`dotnet test`)
- No compilation errors (`dotnet build`)
- Code formatted (`dotnet format`)
- Orleans grains properly configured
- Logging added at key checkpoints

### Orleans-Specific
- Use Orleans TestKit for grain testing
- Configure clustering (MongoDB/Redis)
- Implement event sourcing patterns
- Handle grain lifecycle properly
- Monitor Orleans dashboard (port 8080)
- Leverage **context7** tool for Orleans-specific issues

## Error Resolution Protocol

1. **Identify root cause** through logs/diagnostics
2. **Locate existing patterns** in codebase
3. **Apply minimal fix** following established patterns
4. **Verify fix** with comprehensive tests
5. **Document fix** in commit message

### Orleans Troubleshooting
- Verify grain registration and interfaces
- Check clustering and storage configuration
- Validate event sourcing setup
- Monitor grain activation/deactivation
- Review stream provider configuration

---

**REMEMBER: Analyze → Propose → Confirm → Implement. Always prioritize maintainability, follow TDD, and ask for clarification when uncertain.**