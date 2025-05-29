# Aevatar Station Architecture Document

## 1. Platform Overview

Aevatar Station is a full-stack platform for developing, managing, and deploying AI agents. Its core philosophy is "an agent is a self-referential structure of language," built on distributed, event-driven, and pluggable design principles, supporting flexible extension and high-availability deployment.

### Key Capabilities
- **Agent Creation & Management**: Dynamic creation and lifecycle management of intelligent agents
- **Event-Driven Architecture**: Asynchronous event processing with event sourcing patterns
- **Real-time Communication**: SignalR integration for bidirectional communication
- **Plugin System**: Hot-pluggable extensions via WebHook mechanism
- **Distributed Computing**: Orleans-based virtual actor model for scalability
- **Multi-tenant Support**: Isolated agent environments with fine-grained permissions

---

## 2. Architecture Overview Diagram

```
[Client Applications]
     │
     ├─── SignalR WebSocket ──┐
     └─── HTTP/REST API ──────┤
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                    API Gateway Layer                          │
├───────────────────────────────────────────────────────────────┤
│ • HttpApi.Host (Main API)                                    │
│ • Developer.Host (Developer Tools)                           │
│ • AevatarSignalRHub (Real-time Communication)               │
│ • StationSignalRHub (Notifications)                          │
└──────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                    Orleans Cluster Layer                      │
├───────────────────────────────────────────────────────────────┤
│ • Orleans Silo (Distributed Runtime)                         │
│ • GAgent Grains (Agent Instances)                            │
│ • SignalR Orleans Backplane                                  │
│ • Stream Processing (Kafka/In-Memory)                        │
└──────────────────────────────────────────────────────────────┘
                               │
     ┌─────────────┬───────────┴───────────┬─────────────┐
     ▼             ▼                       ▼             ▼
[Domain Layer] [Application Layer] [CQRS Layer]  [Plugin Layer]
     │             │                       │             │
     ▼             ▼                       ▼             ▼
┌──────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                       │
├───────────────────────────────────────────────────────────────┤
│ • MongoDB (Event Store, State Store, Document Store)         │
│ • Redis (Distributed Cache, Orleans Clustering)              │
│ • Kafka (Event Streaming, Message Bus)                       │
│ • ElasticSearch (State Queries, Analytics, Logs)            │
│ • Qdrant (Vector Database for AI Embeddings)                │
│ • Neo4j (Agent Relationship Graph)                           │
└──────────────────────────────────────────────────────────────┘
```

---

## 3. Layered Structure Description

### 3.1 Infrastructure Layer

#### Storage Systems
- **MongoDB**: 
  - Event sourcing primary storage
  - Orleans grain state persistence
  - User data and permissions
  - Plugin metadata storage
  
- **Redis**: 
  - Orleans cluster membership
  - Distributed cache
  - Session management
  - Real-time coordination
  
- **Kafka**: 
  - Event stream processing
  - Inter-agent communication bus
  - Audit log streaming
  - External integration events
  
- **ElasticSearch**: 
  - CQRS read model storage
  - Full-text search capabilities
  - System metrics and analytics
  - Distributed logging
  
- **Qdrant**: 
  - AI model embeddings
  - Semantic similarity search
  - Knowledge base vectors
  
- **Neo4j**: 
  - Agent relationship graph
  - Hierarchical agent structures
  - Network analysis

#### Orchestration
- **Aspire**: Service orchestration and startup coordination
- **Docker/Kubernetes**: Container orchestration for microservices
- **Dapr**: Service-to-service communication abstraction

### 3.2 Core Service Layer

#### Orleans Framework
- **Orleans Silo**: 
  - Virtual actor runtime
  - Grain activation and lifecycle
  - Distributed state management
  - Stream processing runtime

#### GAgent System (Intelligent Agents)
- **GAgentBase<TState, TEvent>**: Abstract base class for all agents
  - Event sourcing implementation
  - State management with versioning
  - Hierarchical agent relationships
  - Event handler routing
  
- **Agent Types**:
  - **CreatorGAgent**: Manages agent creation and metadata
  - **PublishingGAgent**: Event publishing functionality
  - **GroupGAgent**: Agent grouping and coordination
  - **CodeGAgent**: Dynamic code storage for webhooks
  - **SubscriptionGAgent**: External webhook subscriptions
  - **SignalRGAgent**: Real-time communication proxy

#### Domain Layer
- **Domain.Shared**: 
  - Shared domain models
  - Event definitions
  - State definitions
  - Common interfaces
  
- **Domain.Grains**: 
  - Orleans grain interfaces
  - Agent-specific domain logic
  - Event sourcing events

#### Application Layer
- **Application.Contracts**: 
  - Service interfaces
  - DTOs and view models
  - Permission definitions
  
- **Application.Grains**: 
  - Business logic implementation
  - Agent implementations
  - Event handlers
  
- **Application Services**:
  - AgentService: Agent CRUD operations
  - NotificationService: Push notifications
  - AccountService: User management
  - SubscriptionService: Webhook management

### 3.3 API & External Interface Layer

#### HTTP APIs
- **HttpApi.Host**: 
  - Main RESTful API endpoint
  - JWT authentication
  - Swagger documentation
  - Response wrapping
  
- **Developer.Host**: 
  - Developer tools API
  - Debug endpoints
  - System monitoring

#### Real-time Communication
- **SignalR Integration**:
  - **AevatarSignalRHub**: Agent event streaming
  - **StationSignalRHub**: System notifications
  - Orleans backplane for distributed SignalR
  - Automatic reconnection and scaling

### 3.4 Plugin & Dynamic Extension Layer

#### WebHook System
- **WebHook.Host**: 
  - Dynamic DLL loading
  - Remote code execution
  - Plugin isolation
  - Multi-tenant support
  
- **WebHook.SDK**: 
  - Plugin development framework
  - IWebhookHandler interface
  - Event subscription APIs
  
- **WebHook.Deploy**: 
  - Plugin deployment tools
  - Version management
  - Security scanning

### 3.5 Authentication & Security Layer

#### AuthServer
- **OpenIddict Integration**: OAuth 2.0 and OpenID Connect
- **Multi-factor Authentication**: Email, SMS, Authenticator apps  
- **Role-Based Access Control**: Fine-grained permissions
- **API Key Management**: Service-to-service authentication
- **Security Stamp Validation**: Token revocation support

#### Permission System
- **Dynamic Permissions**: Runtime permission registration
- **Hierarchical Permissions**: Inherited from organization structure
- **Agent-specific Permissions**: Per-agent access control
- **State-based Permissions**: Access based on agent state

### 3.6 Worker & Background Services

#### Worker Services
- **Scheduled Tasks**: Cron-based job execution
- **Event Processing**: Background event handlers
- **Cleanup Jobs**: State and log cleanup
- **Health Checks**: System monitoring

#### Kubernetes Jobs
- **Migration Jobs**: Database schema updates
- **Import/Export Jobs**: Bulk data operations
- **Analytics Jobs**: Report generation

### 3.7 Testing & Development Tools

#### Testing Infrastructure
- **Orleans TestKit**: Unit testing for grains
- **Integration Tests**: Full stack testing
- **Load Testing**: Performance benchmarks
- **Chaos Testing**: Resilience testing

#### Developer Tools
- **Orleans Dashboard**: Grain monitoring UI
- **Developer Logger**: Enhanced logging for development
- **API Documentation**: Auto-generated from code
- **Debug Endpoints**: System introspection

---

## 4. GAgent (Intelligent Agent) Mechanism

### Core Concepts

#### Agent Definition
```csharp
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class MyGAgent : GAgentBase<MyAgentState, MyAgentEvent>
{
    // Agent implementation
}
```

#### Key Features
- **Event Sourcing**: 
  - All state changes stored as immutable events
  - Event replay for state reconstruction
  - Audit trail and time-travel debugging
  
- **State Management**: 
  - Strongly-typed state objects
  - Automatic versioning
  - Conflict resolution
  
- **Stream Communication**: 
  - Pub/sub messaging between agents
  - Event filtering and routing
  - Backpressure handling
  
- **Hierarchical Relationships**: 
  - Parent-child agent relationships
  - Event bubbling and capturing
  - Composite agent patterns

### Agent Lifecycle

1. **Creation**: Via CreatorGAgent with metadata
2. **Configuration**: Dynamic property injection
3. **Activation**: Orleans grain activation
4. **Event Processing**: Handle incoming events
5. **State Transitions**: Apply events to state
6. **Persistence**: Save state snapshots
7. **Deactivation**: Cleanup and resource release

### Event Handling

#### Event Handler Attributes
```csharp
[EventHandler]
public async Task HandleSpecificEvent(SpecificEvent evt) { }

[AllEventHandler]
public async Task HandleAnyEvent(EventWrapperBase evt) { }
```

#### Event Flow
1. Event published to stream
2. Stream delivers to subscribers
3. Agent validates event
4. Handler method invoked
5. State transition applied
6. Confirmation sent

---

## 5. SignalR Real-time Communication

### Architecture

```
Client ←→ SignalR Hub ←→ Orleans SignalR Backplane ←→ GAgent
```

### Implementation Details

#### Hub Configuration
- **AevatarSignalRHub**: Agent event streaming
- **StationSignalRHub**: System notifications  
- **Authentication**: JWT bearer tokens
- **User Mapping**: Sub claim to user ID

#### Message Flow
1. Client connects via WebSocket
2. Hub registers connection in Orleans
3. Agent publishes event
4. SignalRGAgent routes to connections
5. Hub sends to specific clients
6. Client receives typed message

#### Message Types
- **Agent Events**: Real-time agent state changes
- **Notifications**: System alerts and updates
- **Command Responses**: Async command results
- **Progress Updates**: Long-running operation status

---

## 6. CQRS Implementation

### Command Side
- **Commands**: State-changing operations
- **Command Handlers**: Business logic execution
- **Event Store**: MongoDB event persistence
- **Event Projections**: Update read models

### Query Side  
- **Queries**: Read-only operations
- **Query Handlers**: ElasticSearch queries
- **Read Models**: Denormalized views
- **Caching**: Redis cache layer

### State Projector
```csharp
public class AevatarStateProjector : IStateProjector
{
    // Batches state changes
    // Projects to ElasticSearch
    // Handles versioning conflicts
}
```

---

## 7. Technology Stack & Dependencies

### Core Frameworks
- **.NET 9.0**: Latest .NET runtime
- **Orleans 8.x**: Virtual actor framework
- **ABP Framework**: Application framework
- **MediatR**: CQRS implementation

### Data Access
- **MongoDB Driver**: Document database
- **StackExchange.Redis**: Cache client
- **Elastic.Clients**: Search client
- **Neo4j Driver**: Graph database

### Messaging
- **Kafka Streams**: Event streaming
- **SignalR**: Real-time messaging
- **Dapr**: Service mesh

### Observability
- **Serilog**: Structured logging
- **OpenTelemetry**: Distributed tracing
- **Prometheus**: Metrics collection
- **Grafana**: Visualization

### Development
- **Swagger/OpenAPI**: API documentation
- **xUnit**: Testing framework
- **FluentAssertions**: Test assertions
- **Bogus**: Test data generation

---

## 8. Security Architecture

### Authentication Flow
1. Client requests token from AuthServer
2. AuthServer validates credentials
3. JWT issued with claims and roles
4. Client includes token in requests
5. API validates token and claims
6. Security stamp checked for revocation

### Authorization Layers
- **API Level**: Controller action filters
- **Service Level**: Method interceptors  
- **Grain Level**: Grain filters
- **State Level**: Property access control

### Data Protection
- **Encryption at Rest**: MongoDB encryption
- **Encryption in Transit**: TLS 1.3
- **Key Management**: Azure Key Vault / AWS KMS
- **Secrets**: Environment variables

---

## 9. Deployment Architecture

### Container Structure
```
aevatar-station/
├── api-gateway/
│   ├── httpapi-host
│   └── developer-host
├── orleans-cluster/
│   ├── silo-1
│   ├── silo-2
│   └── silo-n
├── infrastructure/
│   ├── mongodb
│   ├── redis
│   ├── kafka
│   └── elasticsearch
└── workers/
    ├── worker-1
    └── worker-n
```

### Kubernetes Resources
- **Deployments**: Stateless services
- **StatefulSets**: Orleans silos
- **Services**: Load balancing
- **ConfigMaps**: Configuration
- **Secrets**: Sensitive data
- **PersistentVolumes**: Data storage

### Scaling Strategy
- **Horizontal**: Add more silos/workers
- **Vertical**: Increase resource limits
- **Auto-scaling**: Based on metrics
- **Geographic**: Multi-region deployment

---

## 10. Monitoring & Operations

### Health Checks
- **/health**: Basic health endpoint
- **/health/ready**: Readiness probe
- **/health/live**: Liveness probe
- **Custom Checks**: Database, cache, etc.

### Metrics
- **Application Metrics**: Business KPIs
- **System Metrics**: CPU, memory, disk
- **Orleans Metrics**: Grain activations
- **Custom Metrics**: Agent-specific

### Logging
- **Structured Logs**: JSON format
- **Log Levels**: Configurable per namespace
- **Correlation IDs**: Request tracing
- **Log Aggregation**: ElasticSearch

### Alerting
- **Threshold Alerts**: Metric-based
- **Anomaly Detection**: ML-based
- **Escalation**: PagerDuty integration
- **Runbooks**: Automated responses

---

## 11. Development Workflow

### Local Development
1. Run infrastructure via Docker Compose
2. Start Orleans silo locally
3. Run API projects
4. Access Orleans Dashboard
5. Use Swagger for API testing

### CI/CD Pipeline
1. **Build**: Compile and package
2. **Test**: Unit and integration tests
3. **Scan**: Security and quality checks
4. **Deploy**: Rolling deployment
5. **Verify**: Smoke tests
6. **Monitor**: Post-deployment checks

### Feature Development
1. Define agent interface
2. Implement grain class
3. Add event handlers
4. Write unit tests
5. Update documentation
6. Create migration if needed

---

## 12. Future Enhancements

### Planned Features
- **GraphQL API**: Alternative query interface
- **Agent Marketplace**: Share agent templates
- **Visual Designer**: Drag-drop agent creation
- **Mobile SDKs**: iOS/Android support
- **Edge Deployment**: Run agents at edge

### Performance Optimizations
- **Grain Placement**: Optimize activation
- **State Caching**: Reduce persistence calls
- **Event Batching**: Improve throughput
- **Query Optimization**: Better indexing

### Security Enhancements
- **Zero Trust**: Service mesh security
- **Audit Logs**: Compliance reporting
- **Data Masking**: PII protection
- **Threat Detection**: Anomaly detection

---
