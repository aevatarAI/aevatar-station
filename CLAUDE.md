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
- @source-control.md - Source control management strategies and workflows
- @technical-documentation-practices.md - Practices for creating and maintaining technical documentation
- @test-case-generation-guidelines.md - Guidelines for generating test cases when working on TDD

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
- ❌ **NEVER** skip writing tests unless explicitly authorized
- ✅ **ALWAYS** analyze and propose BEFORE implementing
- ✅ **ALWAYS** write failing tests FIRST before any implementation
- ✅ **ALWAYS** ask for clarification vs assumptions
- ✅ **ALWAYS** add `ABOUTME:` comments to new files (2 lines)
- ✅ **ALWAYS** preserve unrelated working code
- ✅ **ALWAYS** follow strict TDD: Red → Green → Refactor cycle

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

### STRICT TEST-DRIVEN DEVELOPMENT (TDD) - ABSOLUTELY MANDATORY

#### TDD is NOT OPTIONAL - It is a REQUIREMENT for ALL code changes

**The TDD Cycle MUST be followed for EVERY feature, bug fix, or code modification:**

1. **RED PHASE** - Write failing test FIRST
   - Define expected behavior through test
   - Run test to confirm it fails (proves test is valid)
   - NO implementation code allowed at this stage
   
2. **GREEN PHASE** - Write MINIMAL code to pass
   - Implement ONLY enough code to make test pass
   - No extra features or "nice to have" additions
   - Run test to confirm it passes
   
3. **REFACTOR PHASE** - Improve code quality
   - Clean up implementation while tests stay green
   - Extract methods, improve naming, reduce duplication
   - Run all tests after each change
   
4. **REPEAT** - Continue cycle for next requirement

#### TDD Enforcement Rules

**BEFORE writing ANY implementation code, you MUST:**
- Write at least one failing test
- Run the test to verify it fails correctly
- Show the failing test output
- Only THEN proceed to implementation

**VIOLATIONS of TDD will occur if you:**
- Write implementation code before tests
- Skip writing tests "to save time"
- Write tests after implementation
- Modify code without updating tests first

#### Test Case Documentation (Mandatory)

**During the RED PHASE of TDD, you MUST also:**
- Generate comprehensive test case documentation following @test-case-generation-guidelines.md
- Apply ALL six mandatory test design methods:
  1. **Equivalence Class Partitioning** - Valid/invalid input classes
  2. **Boundary Value Analysis** - Min/max/edge conditions
  3. **Decision Table Testing** - Multiple condition combinations
  4. **Scenario-Based Testing** - Real-world user workflows
  5. **Error Guessing** - Predict potential defects
  6. **State Transition Testing** - State changes and events

**Test Case Documentation Requirements:**
- Create/update markdown files in: `test-cases/{version}/{feature-name}-test-cases.md`
- Follow the hierarchical structure (H1-H6) for XMind compatibility
- Document BEFORE writing the actual test code
- Update documentation when tests change
- Include positive, negative, boundary, and exception cases

**Documentation Example:**
```markdown
# PricingService Test Cases
## Discount Calculation
### VIP Customer Discounts
#### Standard VIP Discount Rules
##### VIP customer receives 10% discount on orders over $100
###### Expected Result
- Discount amount equals 10% of order total
- Final amount is reduced by discount
- Discount is recorded in order history
```

**Test-First Development Example:**
```csharp
// STEP 1: Write failing test FIRST
[Fact]
public async Task Should_CalculateDiscount_When_CustomerIsVIP()
{
    // This test MUST be written BEFORE the CalculateDiscount method exists
    var service = new PricingService();
    var result = await service.CalculateDiscount(customerId: "VIP123", amount: 100);
    
    result.ShouldBe(10); // 10% discount for VIP
}

// STEP 2: Run test - it MUST fail (method doesn't exist yet)
// STEP 3: Write MINIMAL implementation to pass
// STEP 4: Refactor if needed
// STEP 5: Write next failing test for next requirement
```

### Test Coverage (Non-negotiable)
- **Unit tests** - Method-level logic (MUST be written FIRST)
- **Integration tests** - Component interactions (MUST be written FIRST)
- **End-to-end tests** - Full system workflows (MUST be written FIRST)

**IMPORTANT:** Tests must ALWAYS be written BEFORE implementation code.

**The ONLY exception:** User provides explicit authorization:
"I AUTHORIZE YOU TO SKIP WRITING TESTS THIS TIME"

**Without this exact authorization, you MUST follow TDD strictly.**

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

**All categories MUST be documented using the test case generation guidelines before implementation.**

## Development Workflow

### Task Analysis Protocol (MANDATORY)
Before ANY implementation:
1. **Analyze the request** - Understand the full scope and requirements
2. **Use sequentialthinking** - Break down into MECE components
3. **Research existing patterns** - Use openmemory and grep/find tools
4. **Propose solution approach** - Present plan BEFORE coding
5. **Design test cases** - Apply six test design methods from @test-case-generation-guidelines.md
6. **Document test cases** - Generate markdown documentation in test-cases directory
7. **Get confirmation** - Ensure alignment before proceeding
8. **Write failing tests** - Start with RED phase of TDD based on documented cases
9. **Then implement** - Only after tests are written and failing

### Task Execution
1. **Use TodoWrite** for complex multi-step tasks
2. **Analyze existing code** before modifications
3. **Generate test case documentation** using six design methods
4. **Write failing tests FIRST** based on documented test cases
5. **Execute parallel operations** when possible
6. **Implement minimal code** to make tests pass
7. **Refactor** while keeping tests green
8. **Update test documentation** if tests change during refactoring
9. **Validate all tests pass** before considering task complete
10. **Clean up temporary files** at completion

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

**REMEMBER: Analyze → Propose → Confirm → Write Tests → See Tests Fail → Implement → See Tests Pass → Refactor. 

TDD is MANDATORY - NO EXCEPTIONS without explicit authorization. 
Always prioritize maintainability, STRICTLY follow TDD, and ask for clarification when uncertain.

The sequence is ALWAYS: RED (failing test) → GREEN (minimal implementation) → REFACTOR (improve code).**