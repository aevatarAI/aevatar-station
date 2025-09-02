# Technology Steering Document

## Core Technology Stack

### Primary Framework
- **.NET 9.0**: Latest stable version for optimal performance and features
- **Orleans 9.0**: Distributed actor model framework for scalable agent implementations
- **ABP 9.1.0**: Application framework providing modular architecture and cross-cutting concerns

### AI & Machine Learning
- **Microsoft Semantic Kernel 1.57.0-alpha**: Integration framework for AI capabilities
- **Multi-LLM Support**: 
  - OpenAI GPT models
  - Azure AI services
  - Google Gemini
  - Amazon AI services
- **MCP (Model Context Protocol) 0.3.0-preview.3**: External tool integration

### Data & Storage
- **MongoDB**: Primary database for state persistence
- **Redis**: Caching and session management
- **Event Sourcing**: Orleans-based event sourcing for state management

### Communication & Messaging
- **SignalR**: Real-time communication
- **Kafka Streams**: High-throughput event streaming
- **Orleans Streams**: Built-in streaming capabilities

## Architecture Patterns

### Actor Model
All GAgents implement the Orleans actor model:
- **Grain-based Architecture**: Each GAgent is an Orleans grain
- **Virtual Actors**: Automatic instantiation and lifecycle management
- **Location Transparency**: Agents can be deployed across clusters

### Event Sourcing
- **State Management**: All state changes through events
- **Event Replay**: State reconstruction from event history
- **CQRS Support**: Command Query Responsibility Segregation

### Plugin Architecture
- **Modular Design**: Each GAgent type is a separate module
- **Dependency Injection**: Service-based architecture
- **Configuration-driven**: Flexible configuration system

## Key Components

### GAgent Base Classes
- **GAgentBase<TState, TEvent>**: Core foundation for all agents
- **AIGAgentBase<TState, TEvent>**: AI-enhanced agent base with brain system
- **GroupMemberGAgentBase**: Workflow orchestration base class
- **MemberGAgentBase**: Member management base class

### AI Integration System
- **IBrain Interface**: Core abstraction for AI functionality
- **IBrainFactory**: AI service factory pattern
- **Semantic Kernel Integration**: Tool calling and function execution
- **MCP Client Providers**: External tool integration

### Event System
- **EventBase**: Base class for all events
- **StateLogEventBase**: Base class for state change events
- **EventHandler Attribute**: Declarative event handling
- **Publish/Subscribe**: Agent-to-agent communication

### State Management
- **StateBase**: Base class for agent state
- **GenerateSerializer**: Orleans serialization attributes
- **Id Attributes**: Sequential property identification
- **ConfigurationBase**: Configuration management

## Technical Standards

### Coding Standards
- **Language**: C# with modern language features
- **Comments**: All code comments and logs must be in English
- **Naming**: Follow C# naming conventions
- **Architecture**: Clean Architecture principles

### Serialization Requirements
- **Classes Only**: State and events must be classes (not records)
- **GenerateSerializer**: Required for all serializable classes
- **Id Attributes**: Sequential numbering starting from 0
- **Inheritance**: Child classes restart Id numbering from 0

### Event Sourcing Rules
- **Immutability**: Events are immutable once created
- **State Changes**: Only through RaiseEvent() method
- **Confirmation**: Always call ConfirmEvents() after state changes
- **Transition Methods**: Implement GAgentTransitionState for state updates

### AI Integration Standards
- **Brain System**: Use centralized brain management
- **Tool Registration**: Register tools through brain system
- **Configuration**: Centralized LLM configuration
- **Error Handling**: Proper AI service error management

## Development Environment

### Build System
- **Centralized Package Management**: Directory.Packages.props
- **Target Framework**: .NET 9.0 across all projects
- **Build Tools**: Standard .NET CLI tools
- **Package Management**: NuGet with central versioning

### Testing Framework
- **Unit Testing**: xUnit with Shouldly assertions
- **Integration Testing**: Orleans TestKit for grain testing
- **Mocking**: Moq for dependency isolation
- **Test Coverage**: Comprehensive coverage requirements

### CI/CD Pipeline
- **Source Control**: GitHub with conventional commits
- **Build Automation**: GitHub Actions
- **Package Publishing**: Automated MyGet publishing on release tags
- **Quality Gates**: Automated testing and code analysis

## Performance Considerations

### Scalability Requirements
- **Actor Model**: Leverage Orleans for horizontal scaling
- **State Management**: Efficient event sourcing for large state histories
- **Communication**: Optimized inter-agent communication patterns
- **Resource Management**: Proper disposal and cleanup

### Optimization Strategies
- **Async/Await**: Proper asynchronous programming patterns
- **Caching**: Strategic caching for frequently accessed data
- **Batching**: Batch operations where possible
- **Lazy Loading**: Delay resource initialization until needed

### Monitoring & Observability
- **Logging**: Comprehensive logging with correlation IDs
- **Metrics**: Performance metrics collection
- **Diagnostics**: Orleans dashboard integration
- **Health Checks**: Agent health monitoring

## Security Considerations

### Data Protection
- **Sensitive Data**: Proper handling of API keys and credentials
- **Encryption**: Data encryption at rest and in transit
- **Access Control**: Proper authorization and authentication
- **Audit Logging**: Comprehensive audit trails

### AI Service Security
- **API Key Management**: Secure storage and rotation of AI service keys
- **Rate Limiting**: Proper rate limiting for AI service calls
- **Content Filtering**: AI content safety and filtering
- **Data Privacy**: Compliance with data protection regulations

## Integration Patterns

### Platform Integration
- **Aevatar Core**: Deep integration with Aevatar platform services
- **Event System**: Consistent event handling across platform
- **Configuration**: Platform-wide configuration management
- **Monitoring**: Integration with platform monitoring systems

### External Service Integration
- **AI Services**: Standardized integration with multiple AI providers
- **Social Platforms**: Consistent patterns for social media integration
- **Blockchain**: Standardized blockchain interaction patterns
- **Storage**: Unified storage and caching patterns

## Deployment Considerations

### Packaging
- **NuGet Packages**: Standard .NET packaging
- **Version Management**: Semantic versioning
- **Dependency Management**: Clear dependency specifications
- **Platform Compatibility**: Cross-platform support

### Configuration
- **Environment-specific**: Configuration per deployment environment
- **Feature Flags**: Runtime feature toggling
- **Connection Strings**: Secure connection management
- **Settings Validation**: Configuration validation at startup

## Technology Constraints

### Platform Requirements
- **Aevatar Compatibility**: Must work with Aevatar platform specifications
- **Orleans Version**: Locked to Orleans 9.0 for compatibility
- **ABP Framework**: Must use ABP 9.1.0 for consistency
- **.NET Version**: Target .NET 9.0 for latest features

### Development Constraints
- **No Direct Grain Factory**: Must use IGAgentFactory for agent creation
- **No Constructor Injection**: Use service provider pattern for dependencies
- **Event Sourcing Compliance**: All state changes must go through events
- **Serialization Requirements**: Must follow Orleans serialization rules

### Quality Constraints
- **Test Coverage**: High test coverage requirements
- **Documentation**: Comprehensive documentation in English and Chinese
- **Code Quality**: Strict coding standards and best practices
- **Performance**: Must meet platform performance benchmarks