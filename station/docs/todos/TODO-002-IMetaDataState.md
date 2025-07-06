# TODO-002: Create IMetaDataState Interface and Event Classes

## Task Overview
Create a new `Aevatar.MetaData` project within the framework that contains the `IMetaDataState` interface with default Apply implementation using .NET 8+ default interface methods, along with related event classes for agent metadata state management.

## Description
Implement a dedicated project for the foundational interface and event classes that will replace `CreatorGAgentState` with better separation of concerns and automatic event sourcing support. This separation into its own project ensures clean architecture and reusability.

## Acceptance Criteria
- [ ] Create new project `Aevatar.MetaData` in `framework/src/`
- [ ] Create `IMetaDataState` interface with default Apply method
- [ ] Create base `MetaDataStateLogEvent` class
- [ ] Create specific event classes: `AgentCreatedEvent`, `AgentStatusChangedEvent`, `AgentPropertiesUpdatedEvent`, `AgentActivityUpdatedEvent`
- [ ] Add Orleans serialization attributes
- [ ] Ensure compatibility with existing event sourcing pipeline
- [ ] Add project references as needed
- [ ] Add comprehensive unit tests

## Project Structure
```
framework/src/Aevatar.MetaData/
├── Aevatar.MetaData.csproj
├── IMetaDataState.cs
├── Events/
│   ├── MetaDataStateLogEvent.cs
│   ├── AgentCreatedEvent.cs
│   ├── AgentStatusChangedEvent.cs
│   ├── AgentPropertiesUpdatedEvent.cs
│   └── AgentActivityUpdatedEvent.cs
└── Enums/
    └── AgentStatus.cs (if not already in another shared project)
```

## File Locations
- `framework/src/Aevatar.MetaData/Aevatar.MetaData.csproj`
- `framework/src/Aevatar.MetaData/IMetaDataState.cs`
- `framework/src/Aevatar.MetaData/Events/MetaDataStateLogEvent.cs`
- `framework/src/Aevatar.MetaData/Events/AgentCreatedEvent.cs`
- `framework/src/Aevatar.MetaData/Events/AgentStatusChangedEvent.cs`
- `framework/src/Aevatar.MetaData/Events/AgentPropertiesUpdatedEvent.cs`
- `framework/src/Aevatar.MetaData/Events/AgentActivityUpdatedEvent.cs`

## Implementation Details

### IMetaDataState Interface
```csharp
public interface IMetaDataState
{
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string AgentType { get; set; }
    string Name { get; set; }
    Dictionary<string, string> Properties { get; set; }
    GrainId AgentGrainId { get; set; }
    DateTime CreateTime { get; set; }
    AgentStatus Status { get; set; }
    DateTime LastActivity { get; set; }
    
    // Default Apply method implementation (.NET 8+ feature)
    void Apply(MetaDataStateLogEvent @event) { /* implementation */ }
}
```

### Event Classes
All event classes must:
- Inherit from `MetaDataStateLogEvent`
- Use `[GenerateSerializer]` attribute
- Use `[Id(n)]` attributes for properties
- Follow Orleans serialization best practices

## Project Configuration
### Aevatar.MetaData.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Orleans.Sdk" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Aevatar.Core.Abstractions\Aevatar.Core.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

## Dependencies
- Orleans serialization framework
- Existing `StateLogEventBase<T>` pattern from `Aevatar.Core.Abstractions`
- `AgentStatus` enum (to be defined in this project or imported from shared project)

## Testing Requirements
- Create corresponding test project: `framework/test/Aevatar.MetaData.Tests`
- Unit tests for default Apply method with all event types
- Serialization/deserialization tests
- Event application state transition tests
- Null handling and edge case tests

## Success Metrics
- All tests pass
- Compatible with existing event sourcing pipeline
- Performance comparable to current implementation
- No serialization issues in Orleans cluster
- Clean project dependencies without circular references

## Notes
- This interface replaces functionality from `CreatorGAgentState`
- Must maintain compatibility with existing Orleans infrastructure
- Focus on immutability and thread safety
- Consider future extensibility for additional metadata
- As a separate project, it can be referenced by both framework and station projects without creating circular dependencies

## Priority: High
This is foundational for the entire architecture replacement.