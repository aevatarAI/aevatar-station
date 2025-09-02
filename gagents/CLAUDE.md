# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## System Overview

**Aevatar GAgents** - Custom intelligent agent solution built on Microsoft Orleans and ABP Framework, designed to enable developers to create, manage, and deploy distributed AI agents on Aevatar Station.

### Architecture
- **src/** - Core library projects containing agent implementations
  - AI modules (Abstractions, AIGAgent, SemanticKernel)
  - Communication agents (ChatAgent, GroupChat, Router)
  - Integration modules (Twitter, Telegram, AElf blockchain)
- **test/** - Test projects with Orleans TestKit and shared infrastructure
- **simples/** - Sample applications demonstrating usage patterns

**Tech Stack**: .NET 9.0, Orleans 9.0, ABP 9.1.0, MongoDB, Redis, SignalR
**AI Support**: Microsoft Semantic Kernel, OpenAI, Azure AI, Google Gemini, Amazon AI
**Patterns**: Actor Model, Event Sourcing, CQRS, Multi-tenancy, Domain-Driven Design

## Development Commands

```bash
# Build & Test
dotnet build
dotnet test
dotnet format && dotnet build --no-incremental  # Pre-commit

# Run Specific Tests
dotnet test test/[ProjectName] --filter "FullyQualifiedName~[TestClass]" --verbosity normal

# Run Sample Applications
cd simples/AIGAgent/SimpleAIGAgent.Silo && dotnet run
cd simples/AIGAgent/SimpleAIGAgent.Client && dotnet run

# Package Management
dotnet restore
dotnet pack src/[ProjectName] --configuration Release -p:PackageVersion=[VERSION]
```

## Core Concepts

### GAgent Framework
Distributed agents inherit from `GAgentBase<TState, TEvent>`:

```csharp
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class MyAgent : GAgentBase<MyState, MyEvent>, IMyAgent
{
    [EventHandler]
    public async Task HandleEventAsync(SomeEvent @event)
    {
        // Process event and update state
        State.Property = @event.Data;
        await ConfirmEvents();
    }
}
```

### Required Components for Agent Creation
1. **State Class** - Inherits from `StateBase` with `[GenerateSerializer]`
2. **Event Sourcing Event** - Inherits from `SEventBase` 
3. **External Message Event** - Inherits from `EventBase` with `[GenerateSerializer]`
4. **Agent Implementation** - Inherits from `GAgentBase<TState, TEvent>`

### AI Integration
- **IBrain Interface** - Core abstraction for AI functionality
- **SemanticKernel Module** - Integrations with Azure OpenAI, Google Gemini
- **Configuration** - AI services configured via ABP settings

### Event Flow
Client → SignalR Hub → Orleans Grains → Event Sourcing → State Persistence

## Technical Documentation References
- @~/README.md - Getting started and agent creation tutorial
- @~/md/project-structure.md - Detailed module breakdown
- @~/docs/* - Module-specific documentation

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
- ❌ **NEVER** write implementation code before writing tests
- ✅ **ALWAYS** analyze and propose BEFORE implementing
- ✅ **ALWAYS** ask for clarification vs assumptions
- ✅ **ALWAYS** add `ABOUTME:` comments to new files (2 lines)
- ✅ **ALWAYS** preserve unrelated working code
- ✅ **ALWAYS** follow TDD cycle: Test → Code → Refactor

### Implementation Standards
```csharp
// File header format (required for new files)
// ABOUTME: This file implements [core functionality]
// ABOUTME: [Brief description of purpose/responsibility]

// Required attributes for serialization
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Data { get; set; }
}

// Agent pattern with required attributes
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
**CRITICAL: Test-Driven Development is NON-NEGOTIABLE for ALL implementations**

1. **Write failing test** - Define exact behavior before any code
2. **Run test to confirm failure** - Verify test actually fails with meaningful error
3. **Write minimal code** - Only enough to make the test pass
4. **Run test to confirm pass** - Verify implementation works
5. **Refactor while keeping tests green** - Improve code quality
6. **Repeat cycle** - Continue for each new behavior
7. **Commit** - git commit when implementation is completed and all test cases passed

**TDD Enforcement Rules:**
- ❌ **NEVER** write implementation code before writing tests
- ❌ **NEVER** skip TDD cycle even for "simple" changes
- ❌ **NEVER** write multiple features without tests
- ✅ **ALWAYS** start with a failing test that describes the behavior
- ✅ **ALWAYS** run tests after each step to verify state
- ✅ **ALWAYS** keep implementation minimal until refactoring phase

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
3. **Apply TDD cycle** - Tests BEFORE any implementation code
4. **Execute parallel operations** when possible
5. **Validate with tests** after implementation
6. **Clean up temporary files** at completion

### Development Tools
- **sequentialthinking** - Break down complex tasks using MECE (Mutually Exclusive, Collectively Exhaustive) principles
- **context7** - Orleans-specific guidance, patterns, and troubleshooting support
- **gh** - This is a github CLI. You can use it for git commands to create PR, commit, push etc.

### Quality Gates
- **TDD cycle completed** - Tests written BEFORE implementation
- All tests pass (`dotnet test`)
- No compilation errors (`dotnet build`)
- Code formatted (`dotnet format`)
- Orleans grains properly configured with required attributes
- Logging added at key checkpoints
- Test coverage meets requirements (Unit + Integration + E2E)

### Orleans-Specific
- Use Orleans TestKit for grain testing
- Configure clustering (MongoDB/Redis)
- Implement event sourcing patterns with proper attributes
- Handle grain lifecycle properly
- Monitor Orleans dashboard (port 8080)
- Leverage **context7** tool for Orleans-specific issues
- Always include `[GenerateSerializer]` and `[Id(n)]` attributes

#### Orleans Serialization Requirements (CRITICAL)

**GenerateSerializer Attribute:**
- **MANDATORY** on all classes that need Orleans serialization
- Required for: State classes, Event classes, DTOs, Enums
- Place `[GenerateSerializer]` directly above class/enum declaration

**Id Attribute Management:**
- Every serializable property MUST have `[Id(n)]` attribute
- Id numbers start at 0 within each class inheritance level
- Must be sequential (0, 1, 2, 3...) within the same class
- Child classes restart Id numbering from 0 (independent of parent)
- **NEVER** change existing Id numbers - breaks backward compatibility
- **NEVER** remove Id attributes - breaks deserialization
- Leave intentional gaps for future properties if needed

**Inheritance Patterns:**
```csharp
// Base class - starts at Id(0)
[GenerateSerializer]
public class StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public DateTime CreatedAt { get; set; }
}

// Child class - restarts at Id(0)
[GenerateSerializer]
public class MyAgentState : StateBase
{
    [Id(0)] public string Name { get; set; }        // Child's Id(0)
    [Id(1)] public int Count { get; set; }          // Child's Id(1)
}
```

**Complex Type Serialization:**
- Collections: `[Id(n)] public List<MyType> Items { get; set; }`
- Dictionaries: `[Id(n)] public Dictionary<string, MyType> Map { get; set; }`
- Nested objects: `[Id(n)] public MyComplexType Config { get; set; }`
- All referenced types must also have `[GenerateSerializer]`

**Enum Serialization:**
```csharp
[GenerateSerializer]
public enum MyEnum
{
    Value1,
    Value2
}
```

**Compatibility Rules:**
- Adding new properties: Use next available Id number
- Removing properties: Keep Id number reserved, mark as `[Obsolete]`
- Renaming properties: Allowed (only property name changes)
- Changing property types: Requires migration strategy

## Error Resolution Protocol

1. **Identify root cause** through logs/diagnostics
2. **Locate existing patterns** in codebase
3. **Apply minimal fix** following established patterns
4. **Verify fix** with comprehensive tests
5. **Document fix** in commit message

### Orleans Troubleshooting
- Verify grain registration and interfaces
- Check clustering and storage configuration
- Validate event sourcing setup and attributes
- Monitor grain activation/deactivation
- Review stream provider configuration
- Ensure serialization attributes are present

### Common Dependency Sources
- MyGet Feed: https://www.myget.org/F/aelf-project-dev/api/v3/index.json
- Required packages: Aevatar.Core, Aevatar.EventSourcing.Core, Aevatar.Core.Abstractions

---

**REMEMBER: Analyze → Propose → Confirm → Implement. Always prioritize maintainability, follow TDD, and ask for clarification when uncertain.**