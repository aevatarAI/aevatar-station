# Aevatar Station Architecture Document

## 1. Platform Overview

Aevatar Station is a full-stack platform for developing, managing, and deploying AI agents. Its core philosophy is "an agent is a self-referential structure of language," built on distributed, event-driven, and pluggable design principles, supporting flexible extension and high-availability deployment.

---

## 2. Architecture Overview Diagram

```
[Client/Frontend]
     │
     ▼
┌──────────────────────────────────────────────────────────────┐
│ [API Gateway/HttpApi.Host/Developer.Host]                   │
│ [WebHook.Host (Plugin/Dynamic Extension)]                   │
│ [Worker] (Scheduled Task)                                    │
└──────────────────────────────────────────────────────────────┘
     │             │                │
     ▼             ▼                ▼
[Orleans Silo/Grains]         [Application]           
     │                        │
     ▼                        ▼
[Domain/Domain.Grains]      [AuthServer]
     │                        ▲
     ▼                        │
[MongoDB/Redis/Kafka/Elastic] [DbMigrator]
```

---

## 3. Layered Structure Description

### 3.1 Infrastructure Layer
- **MongoDB**: Event sourcing and state persistence
- **Redis**: Distributed cache and cluster coordination
- **Kafka**: Event streaming and message broadcasting
- **ElasticSearch**: Search and analytics
- **Qdrant**: Vector database for AI embeddings
- **Aspire**: Unified orchestration and service startup

### 3.2 Core Service Layer
- **Orleans Silo/Grains**: Distributed agent and event stream core
- **CQRS**: Command and query separation for scalability
- **Domain/Domain.Grains**: Domain modeling and business rules
- **Application/Application.Grains**: Business logic implementation
- **Kubernetes/Worker**: Automated operations and asynchronous tasks

### 3.3 API & External Interface Layer
- **HttpApi.Host/Developer.Host**: RESTful APIs, bridge between frontend and backend
- **SignalR**: Real-time communication, supports event stream interaction between frontend and agents

### 3.4 Plugin & Dynamic Extension Layer
- **WebHook.Host**: Supports remote DLL injection, dynamic module loading, and polymorphic discovery of IWebhookHandler, enabling hot-plug and multi-tenant isolation

### 3.5 Authentication & Security Layer
- **AuthServer**: Unified authentication and authorization, supports multi-chain identity and encryption

### 3.6 Testing & Tools Layer
- **test directory**: Unit and integration tests
- **DbMigrator**: Database migration and initialization

---

## 4. GAgent (Intelligent Agent) Mechanism

- **Event Sourcing**: All state changes are stored as events in the Event Store (MongoDB/In-Memory)
- **State Management**: Rebuilds current state by replaying events, persisted in the State Store
- **Stream Communication**: Uses Stream Provider (Kafka/In-Memory) for inter-agent message broadcasting and subscription
- **Hierarchical Relationships**: Supports agent registration, subscription, and composition, forming an agent network
- **Extensibility**: GAgent is an abstract base class, supporting various agent types and custom extensions


---

## 6. Technology Stack & Dependencies

- **Core Frameworks**: .NET 9.0, Orleans, ABP, Dapr
- **Persistence**: MongoDB, Redis
- **Message Streaming**: Kafka, Orleans Streams
- **Search/Analytics**: ElasticSearch
- **AI Embedding**: Qdrant
- **Infrastructure Orchestration**: Aspire, Docker
- **Logging/Monitoring**: Serilog, OpenTelemetry

---

## 7. Typical Data Flows & Interaction Processes

### 7.1 SignalR Event Stream
- The frontend connects to the API via SignalR; events are routed through the Orleans cluster to the target GAgent, processed, and responded back to the frontend

### 7.2 Event Sourcing & State Management
- GAgent records events via LogConsistencyProvider; State Store persists the current state, supporting event replay and state recovery

### 7.3 Inter-Agent Communication
- Uses Stream Provider (Kafka) for agent-to-agent publish/subscribe and message broadcasting

---

## 8. Extensibility & Hot-Plug Design

- Plugin mechanism supports remote dynamic loading and polymorphic extension
- Microservice architecture supports horizontal scaling and high availability
- Configuration and dependency injection support multi-environment and flexible switching
- Event-driven and CQRS patterns enhance system decoupling and maintainability
