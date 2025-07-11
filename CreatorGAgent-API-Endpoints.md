# CreatorGAgent API Endpoints Documentation

## Overview

The CreatorGAgent class serves as a central coordination layer for agent management in the Aevatar Station platform. This document details all API endpoints that interact with the CreatorGAgent, organized by controller and functionality.

## Agent Management API (`/api/agent`)

### Agent Type Information

**GET** `/api/agent/agent-type-info-list`
- **Purpose**: Retrieve all available agent types
- **CreatorGAgent Usage**: Indirect through AgentService
- **Response**: List of agent type definitions

### Agent Instance Management

**GET** `/api/agent/agent-list`
- **Purpose**: Get paginated list of all agent instances
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` for each agent
- **Parameters**: Pagination parameters
- **Response**: Paginated list of agent states

**POST** `/api/agent`
- **Purpose**: Create a new agent instance
- **CreatorGAgent Usage**: Calls `CreateAgentAsync(AgentData)`
- **Request Body**: AgentData object containing agent properties
- **Response**: Created agent information

**GET** `/api/agent/{guid}`
- **Purpose**: Retrieve specific agent by ID
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` on target agent
- **Parameters**: Agent GUID
- **Response**: Agent state information

**PUT** `/api/agent/{guid}`
- **Purpose**: Update existing agent properties
- **CreatorGAgent Usage**: Calls `UpdateAgentAsync(UpdateAgentInput)`
- **Parameters**: Agent GUID
- **Request Body**: UpdateAgentInput with new properties
- **Response**: Updated agent information

**DELETE** `/api/agent/{guid}`
- **Purpose**: Delete an agent instance
- **CreatorGAgent Usage**: Calls `DeleteAgentAsync()`
- **Parameters**: Agent GUID
- **Response**: Deletion confirmation

### Agent Relationship Management

**GET** `/api/agent/{guid}/relationship`
- **Purpose**: Get agent relationships (parent/children)
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` to retrieve relationship data
- **Parameters**: Agent GUID
- **Response**: Relationship hierarchy information

**POST** `/api/agent/{guid}/add-subagent`
- **Purpose**: Add sub-agents to an agent
- **CreatorGAgent Usage**: Calls `UpdateAvailableEventsAsync()` to update event subscriptions
- **Parameters**: Agent GUID
- **Request Body**: List of sub-agent IDs
- **Response**: Updated relationship information

**POST** `/api/agent/{guid}/remove-subagent`
- **Purpose**: Remove specific sub-agents
- **CreatorGAgent Usage**: Updates agent relationships through `GetAgentAsync()`
- **Parameters**: Agent GUID
- **Request Body**: List of sub-agent IDs to remove
- **Response**: Updated relationship information

**POST** `/api/agent/{guid}/remove-all-subagent`
- **Purpose**: Remove all sub-agents from an agent
- **CreatorGAgent Usage**: Bulk relationship updates through `GetAgentAsync()`
- **Parameters**: Agent GUID
- **Response**: Updated relationship information

### Event Publishing

**POST** `/api/agent/publishEvent`
- **Purpose**: Publish events through agents
- **CreatorGAgent Usage**: Calls `PublishEventAsync<T>(T event)`
- **Request Body**: Event data with agent ID
- **Response**: Event publication confirmation

## Subscription Management API (`/api/subscription`)

### Event Management

**GET** `/api/subscription/events/{guid}`
- **Purpose**: Get available events for an agent
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` to retrieve event descriptions
- **Parameters**: Agent GUID
- **Response**: List of available event types and descriptions

### Subscription Operations

**POST** `/api/subscription`
- **Purpose**: Create a new event subscription
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` for agent validation
- **Request Body**: Subscription configuration
- **Response**: Created subscription information

**DELETE** `/api/subscription/{subscriptionId}`
- **Purpose**: Cancel an existing subscription
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` to validate subscription ownership
- **Parameters**: Subscription GUID
- **Response**: Cancellation confirmation

**GET** `/api/subscription/{subscriptionId}`
- **Purpose**: Get subscription status
- **CreatorGAgent Usage**: Calls `GetAgentAsync()` to retrieve subscription details
- **Parameters**: Subscription GUID
- **Response**: Subscription status and configuration

## Key CreatorGAgent Methods Used by Endpoints

### Core Agent Operations
- **`GetAgentAsync()`** - Retrieves current agent state (`CreatorGAgentState`)
- **`CreateAgentAsync(AgentData)`** - Creates new agent with initial configuration
- **`UpdateAgentAsync(UpdateAgentInput)`** - Updates agent properties and configuration
- **`DeleteAgentAsync()`** - Removes agent and cleans up resources

### Event Management
- **`PublishEventAsync<T>(T event)`** - Publishes typed events through the agent
- **`UpdateAvailableEventsAsync(List<Type>)`** - Updates the list of available event types

## Data Flow Architecture

```
HTTP Request → Controller → Application Service → CreatorGAgent → Orleans Grain → Event Store
```

1. **HTTP Controllers** receive API requests
2. **Application Services** (`AgentService`, `SubscriptionAppService`) handle business logic
3. **CreatorGAgent** manages agent state and event sourcing
4. **Orleans Grains** provide distributed computing capabilities
5. **Event Store** persists state changes and events

## Authentication and Authorization

All endpoints require appropriate authentication and authorization based on the ABP Framework's permission system. Agent operations are typically scoped to the authenticated user's tenant and permissions.

## Error Handling

The API endpoints implement comprehensive error handling for:
- Invalid agent IDs
- Permission violations
- Concurrent modification conflicts
- Event publishing failures
- Subscription management errors

## Performance Considerations

- Agent state retrieval is optimized through Orleans grain caching
- Event publishing uses asynchronous processing
- Relationship queries are optimized for hierarchical data structures
- Subscription management includes efficient event filtering

## Implementation Details

### File Locations
- **CreatorGAgent**: `station/src/Aevatar.Application.Grains/Agents/Creator/CreatorGAgent.cs:16`
- **AgentController**: `station/src/Aevatar.HttpApi/Controllers/AgentController.cs`
- **SubscriptionController**: `station/src/Aevatar.HttpApi/Controllers/SubscriptionController.cs`
- **AgentService**: `station/src/Aevatar.Application/Service/AgentService.cs:240`
- **SubscriptionAppService**: `station/src/Aevatar.Application/Service/SubscriptionAppService.cs:62`

### Key Integration Points
- **Line 240**: AgentService gets CreatorGAgent grain instance
- **Line 248**: AgentService calls `CreateAgentAsync()`
- **Line 340**: AgentService calls `GetAgentAsync()` for state retrieval
- **Line 360**: AgentService calls `UpdateAgentAsync()` for updates
- **Line 484**: AgentService calls `UpdateAvailableEventsAsync()` for event management
- **Line 625**: AgentService calls `DeleteAgentAsync()` for deletion
- **Line 175**: SubscriptionAppService calls `PublishEventAsync()` for event publishing