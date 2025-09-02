# Project Structure Steering Document

## Repository Organization

### Root Structure
```
aevatar-gagents/
├── src/                          # Core library projects
├── test/                         # Test projects
├── simples/                      # Sample applications
├── docs/                         # Technical documentation
├── .claude/                      # Claude-specific configuration
├── AevatarGAgents.sln            # Solution file
├── Directory.Build.props         # Build properties
├── Directory.Packages.props      # Central package management
└── README.md                     # Project overview
```

### Source Code Organization (src/)
```
src/
├── Aevatar.GAgents.Basic/        # Basic GAgent implementations
│   ├── BasicGAgents/
│   │   ├── GroupGAgent/
│   │   └── PublishGAgent/
│   ├── BasicGEvent/
│   │   └── SocialGEvent/
│   └── Common/
├── Aevatar.GAgents.AIGAgent/     # AI GAgent framework
│   ├── Agent/
│   │   ├── AIGAgentBase.cs
│   │   ├── AIGAgentBase.ChatWithTools.cs
│   │   ├── AIGAgentBase.HttpAsync.cs
│   │   ├── AIGAgentBase.MCP.cs
│   │   ├── AIGAgentBase.Stream.cs
│   │   ├── AIGAgentBase.TextToImage.cs
│   │   ├── AIGAgentBase.Tools.cs
│   │   └── IAIGAgent.cs
│   ├── Dtos/
│   ├── Exceptions/
│   ├── Feature/
│   └── GEvents/
├── Aevatar.GAgents.GroupChat/    # Group chat and workflow
│   ├── BlackboardGAgent.cs
│   ├── CoordinatorGAgentBase.cs
│   ├── GroupMemberGAgentBase.cs
│   └── WorkflowCoordinatorGAgent.cs
├── Aevatar.GAgents.Social/        # Social media agents
│   ├── Aevatar.GAgents.Twitter/
│   └── Aevatar.GAgents.Telegram/
├── Aevatar.GAgents.AI.Abstractions/ # AI abstractions
│   ├── Brain/
│   ├── BrainFactory/
│   ├── Common/
│   └── Options/
├── Aevatar.GAgents.SemanticKernel/ # Semantic Kernel integration
│   ├── Brain/
│   ├── BrainFactory/
│   ├── Extensions/
│   └── VectorStores/
└── Aevatar.GAgents.[NewFeature]/  # New GAgent implementations
```

### Test Organization (test/)
```
test/
├── Aevatar.GAgents.TestBase/     # Shared test infrastructure
├── Aevatar.GAgents.AIGAgent.Test/ # AI GAgent tests
├── Aevatar.GAgents.GroupChat.Test/ # Group chat tests
├── Aevatar.GAgents.[Feature].Test/ # Feature-specific tests
└── OrleansTestKit/               # Orleans testing utilities
```

### Sample Applications (simples/)
```
simples/
├── AIGAgent/
│   ├── SimpleAIGAgent.Client/
│   ├── SimpleAIGAgent.Grains/
│   └── SimpleAIGAgent.Silo/
└── GroupChat/
    ├── GroupChat.Client/
    ├── GroupChat.Grains/
    └── GroupChat.Silo/
```

## GAgent Implementation Standards

### File Organization Pattern
Each GAgent implementation should follow this structure:

```
Aevatar.GAgents.[Feature]/
├── Aevatar.GAgents.[Feature].csproj
├── [Feature]GAgent.cs              # Main implementation file
├── [Feature]GAgentState.cs          # State definition
├── [Feature]GEvent.cs              # Events
├── SEvents/                        # State log events
│   ├── [Feature]StateLogEvent.cs
│   └── [Feature]OperationEvent.cs
├── I[Feature]GAgent.cs             # Interface definition
└── Options/                        # Configuration classes
    └── [Feature]Options.cs
```

### Single File Pattern (Preferred)
For smaller GAgents, keep all related code in a single file:

```csharp
// Aevatar.GAgents.[Feature]/[Feature]GAgent.cs
public interface IMyGAgent : IStateGAgent<MyState> { }

[GenerateSerializer]
public class MyState : StateBase { }

[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent> { }

[GAgent]
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent { }
```

### Required Components
Every GAgent implementation must include:

1. **Interface**: Inherits from `IStateGAgent<TState>` or `IStateGAgent<TState> + IAIGAgent`
2. **State Class**: Inherits from `StateBase` with `[GenerateSerializer]`
3. **State Log Event**: Base event class with `[GenerateSerializer]`
4. **GAgent Implementation**: With `[GAgent]` attribute
5. **Events**: External message events inheriting from `EventBase`

## Naming Conventions

### Project Names
- **Pattern**: `Aevatar.GAgents.[Feature]`
- **Examples**: 
  - `Aevatar.GAgents.Twitter`
  - `Aevatar.GAgents.AIGAgent`
  - `Aevatar.GAgents.GroupChat`

### Class Names
- **GAgent Classes**: `[Feature]GAgent` (e.g., `TwitterGAgent`)
- **State Classes**: `[Feature]GAgentState` (e.g., `TwitterGAgentState`)
- **Interfaces**: `I[Feature]GAgent` (e.g., `ITwitterGAgent`)
- **Events**: `[Feature]GEvent` (e.g., `TwitterGEvent`)
- **State Log Events**: `[Feature]StateLogEvent` (e.g., `TwitterStateLogEvent``

### Namespace Organization
```csharp
// Main namespace
namespace Aevatar.GAgents.[Feature]

// Sub-namespaces
namespace Aevatar.GAgents.[Feature].GEvents
namespace Aevatar.GAgents.[Feature].SEvents
namespace Aevatar.GAgents.[Feature].Options
```

## New GAgent Implementation Guidelines

### Placement Decision Tree

1. **Can it fit in existing project?**
   - Yes → Add to appropriate existing project
   - No → Continue to next step

2. **Is it a basic utility GAgent?**
   - Yes → Add to `Aevatar.GAgents.Basic`
   - No → Continue to next step

3. **Is it a major new feature?**
   - Yes → Create new `Aevatar.GAgents.[Feature]` project
   - No → Re-evaluate existing projects

### New Project Creation Process

When creating a new GAgent project:

1. **Create Project Structure**:
   ```bash
   mkdir src/Aevatar.GAgents.[Feature]
   ```

2. **Create Project File**:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
       <PropertyGroup>
           <TargetFramework>$(DefaultTargetFramework)</TargetFramework>
           <Nullable>enable</Nullable>
           <RootNamespace>Aevatar.GAgents.[Feature]</RootNamespace>
       </PropertyGroup>
       <!-- Dependencies -->
   </Project>
   ```

3. **Add to Solution**:
   ```bash
   dotnet sln AevatarGAgents.sln add src/Aevatar.GAgents.[Feature]/Aevatar.GAgents.[Feature].csproj
   ```

4. **Update Dependencies**: Add references to required packages and projects

5. **Create Test Project**: Add corresponding test project

### Project Dependencies

#### Core Dependencies (All Projects)
```xml
<ItemGroup>
    <PackageReference Include="Aevatar.Core" />
    <PackageReference Include="Aevatar.Core.Abstractions" />
    <PackageReference Include="Aevatar.EventSourcing.Core" />
</ItemGroup>
```

#### Common Project References
```xml
<ItemGroup>
    <ProjectReference Include="..\Aevatar.GAgents.Basic\Aevatar.GAgents.Basic.csproj" />
    <ProjectReference Include="..\Aevatar.GAgents.AI.Abstractions\Aevatar.GAgents.AI.Abstractions.csproj" />
</ItemGroup>
```

#### AI-Specific Dependencies
```xml
<ItemGroup>
    <ProjectReference Include="..\Aevatar.GAgents.AIGAgent\Aevatar.GAgents.AIGAgent.csproj" />
    <ProjectReference Include="..\Aevatar.GAgents.SemanticKernel\Aevatar.GAgents.SemanticKernel.csproj" />
</ItemGroup>
```

## Documentation Standards

### Code Documentation
- **Language**: All code comments and logs must be in English
- **XML Documentation**: Public interfaces and classes require XML comments
- **Method Documentation**: Document parameters, return values, and exceptions
- **Example Documentation**: Provide usage examples in comments

### File Headers
Each new file should include:
```csharp
// ABOUTME: This file implements [core functionality]
// ABOUTME: [Brief description of purpose/responsibility]
```

### README Files
- **Project README**: Each project should have a README.md explaining purpose and usage
- **Sample Documentation**: Samples should include setup and usage instructions
- **API Documentation**: Comprehensive API documentation for public interfaces

### Language Requirements
- **Primary**: English (all code, comments, logs)
- **Secondary**: Chinese (documentation translations)
- **Consistency**: Maintain consistent terminology across all documentation

## Testing Structure

### Test Project Organization
```
test/Aevatar.GAgents.[Feature].Test/
├── Aevatar.GAgents.[Feature].Test.csproj
├── [Feature]TestBase.cs           # Test base class
├── Tests/
│   ├── [Feature]BasicTests.cs     # Basic functionality tests
│   ├── [Feature]IntegrationTests.cs # Integration tests
│   └── [Feature]AITests.cs        # AI-specific tests (if applicable)
└── Mocks/                         # Test mocks and fakes
```

### Test Naming Conventions
- **Test Classes**: `[Feature]Tests` (e.g., `TwitterGAgentTests`)
- **Test Methods**: `MethodName_Scenario_ExpectedResult` (e.g., `SendMessageAsync_ValidMessage_ShouldSucceed`)
- **Test Data**: `[Feature]TestDataFactory` (e.g., `TwitterTestDataFactory`)

## Configuration Management

### Configuration Files
- **appsettings.json**: Application configuration
- **appsettings.Development.json**: Development-specific settings
- **appsettings.Production.json**: Production-specific settings

### Options Pattern
Each GAgent should use the options pattern for configuration:
```csharp
[GenerateSerializer]
public class [Feature]Options : ConfigurationBase
{
    [Id(0)] public string Setting1 { get; set; } = string.Empty;
    [Id(1)] public int Setting2 { get; set; }
}
```

## Build and Packaging

### Package Configuration
```xml
<PropertyGroup>
    <PackageId>Aevatar.GAgents.[Feature]</PackageId>
    <Title>Aevatar [Feature] GAgent</Title>
    <Description>Description of the [Feature] GAgent implementation</Description>
    <PackageTags>aevatar gagent [feature]</PackageTags>
    <RepositoryUrl>https://github.com/aevatarAI/aevatar-gagents</RepositoryUrl>
    <PackageProjectUrl>https://github.com/aevatarAI/aevatar-gagents</PackageProjectUrl>
</PropertyGroup>
```

### Version Management
- **Semantic Versioning**: Follow semantic versioning (Major.Minor.Patch)
- **Central Management**: Version managed through Directory.Packages.props
- **Release Process**: GitHub releases trigger automated MyGet publishing

## Quality Standards

### Code Quality
- **Consistency**: Follow existing code patterns and conventions
- **Performance**: Optimize for scalability and efficiency
- **Maintainability**: Write clean, readable, and maintainable code
- **Testing**: Maintain high test coverage with quality tests

### Documentation Quality
- **Completeness**: Comprehensive documentation for all public APIs
- **Clarity**: Clear and concise explanations
- **Examples**: Practical usage examples
- **Accessibility**: Documentation available in both English and Chinese

### Integration Quality
- **Platform Compatibility**: Ensure compatibility with Aevatar platform
- **Dependency Management**: Clear and well-managed dependencies
- **Error Handling**: Robust error handling and logging
- **Performance**: Meet performance benchmarks and requirements