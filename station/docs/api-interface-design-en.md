# AevatarStation API Interface Design Document

## Overview

AevatarStation is a full-stack platform for developing, managing, and deploying AI agents, built on the core philosophy that "an agent is a self-referential structure of language." The platform employs distributed, event-driven, and pluggable design principles, supporting flexible extension and high-availability deployment.

### Platform Architecture
- **Orleans-based Distributed System**: Leverages Microsoft Orleans for actor-model-based distributed computing
- **Event Sourcing & CQRS**: All agent state changes are stored as events with command-query separation
- **Multi-Silo Architecture**: Three specialized Orleans silos (Scheduler, Projector, User) for different workloads
- **Hot-Pluggable Extensions**: WebHook.Host supports dynamic DLL loading and polymorphic IWebhookHandler discovery
- **Real-time Communication**: SignalR integration for live agent-frontend interaction

### Basic Information
- **Base URL**: `/api`
- **Authentication**: Bearer Token (JWT) via AuthServer
- **Content Type**: `application/json`
- **API Version**: v1
- **Service Endpoints**:
  - HttpApi.Host: `http://localhost:7002` (Main API with Swagger UI)
  - Developer.Host: `http://localhost:7003` (Developer API with Swagger UI)
  - AuthServer: `http://localhost:7001` (Authentication service)

### Infrastructure Components
- **MongoDB**: Event sourcing and state persistence
- **Redis**: Distributed cache and cluster coordination  
- **Kafka**: Event streaming and inter-agent communication
- **ElasticSearch**: Search and analytics capabilities
- **Qdrant**: Vector database for AI embeddings
- **Aspire**: Unified orchestration and service management

### Common Response Format
All interfaces follow standard RESTful response formats with Orleans grain-based processing, including comprehensive error handling, pagination support, and event sourcing capabilities.

## Table of Contents

- [Overview](#overview)
- [1. Agent System Module - 12 APIs](#1-agent-system-module)  
- [2. Plugin Management Module - 4 APIs](#2-plugin-management-module)
- [3. Notification Management Module - 7 APIs](#3-notification-management-module)
- [4. Webhook Management Module - 4 APIs](#4-webhook-management-module)
- [5. Subscription Management Module - 4 APIs](#5-subscription-management-module)
- [6. API Key Management Module - 4 APIs](#6-api-key-management-module)
- [7. API Request Statistics Module - 1 API](#7-api-request-statistics-module)
- [8. Query Service Module - 1 API](#8-query-service-module)

---

## 1. Agent System Module (GAgent)

### Base Path: `/api/agent`

The GAgent (Intelligent Agent) system is the core of AevatarStation, implementing the philosophy that "an agent is a self-referential structure of language." Each agent operates as an Orleans grain with the following characteristics:

- **Event Sourcing**: All state changes are stored as events in MongoDB Event Store
- **State Management**: Current state is rebuilt by replaying events, persisted in State Store
- **Stream Communication**: Uses Kafka Stream Provider for inter-agent message broadcasting and subscription
- **Hierarchical Relationships**: Supports agent registration, subscription, and composition forming an agent network
- **Extensibility**: GAgent is an abstract base class supporting various agent types and custom extensions

### 1.1 Get All Agent Types
**Method**: GET  
**URL**: `/agent-type-info-list`

**Description**: Retrieve information about all available agent types in the system, including type names and parameter definitions, used for selecting appropriate types when creating agents.

**Request Parameters**: None

**Authorization**: Required

**Response Example**:
```json
[
  {
    "id": "OpenAIAgent",
    "name": "OpenAI Agent",
    "description": "Intelligent agent based on OpenAI",
    "parameters": [
      {
        "name": "apiKey",
        "type": "string",
        "required": true,
        "description": "OpenAI API key"
      },
      {
        "name": "model",
        "type": "string", 
        "required": false,
        "description": "Model name to use"
      }
    ]
  }
]
```

### 1.2 Get Agent Instance List
**Method**: GET
**URL**: `/agent-list`

**Description**: Retrieve a paginated list of the current user's agent instances, supporting viewing all created agents and their status.

**Request Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | int | ❌ | 0 | Page index, starting from 0 |
| pageSize | int | ❌ | 20 | Page size, maximum 100 |

**Authorization**: Required

**Request Example**: `GET /api/agent/agent-list?pageIndex=0&pageSize=10`

**Response Example**:
```json
[
  {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "agentType": "OpenAIAgent",
    "name": "My AI Assistant",
    "properties": {
      "apiKey": "sk-***",
      "model": "gpt-4"
    },
    "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
    "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
    "businessAgentGrainId": "business_agent_123"
  }
]
```

### 1.3 Create Agent
**Method**: POST
**URL**: `/`

**Description**: Create a new AI agent instance, requiring specification of agent type and related configuration parameters.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| agentId | Guid? | ❌ | Specify agent ID, auto-generated if not provided |
| agentType | string | ✅ | Agent type, selected from agent type list |
| name | string | ✅ | Agent name |
| properties | Dictionary<string, object>? | ❌ | Agent configuration parameters, provided according to agent type requirements |

**Authorization**: Required

**Request Example (OpenAI Agent)**:
```json
{
  "agentType": "OpenAIAgent",
  "name": "My AI Assistant",
  "properties": {
    "apiKey": "sk-proj-xxxxxxxxxxxx",
    "model": "gpt-4",
    "temperature": 0.7
  }
}
```

**Request Example (Workflow Coordinator)**:
```json
{
  "agentType": "Aevatar.GAgents.GroupChat.WorkflowCoordinator.WorkflowCoordinatorGAgent",
  "name": "Data Processing Workflow",
  "properties": {
    "workflowUnitList": [
      {
        "grainId": "grain1",
        "nextGrainId": "grain2",
        "extendedData": {
          "taskType": "dataValidation",
          "timeout": 30
        }
      },
      {
        "grainId": "grain2",
        "nextGrainId": "grain3",
        "extendedData": {
          "taskType": "dataTransformation",
          "timeout": 60
        }
      },
      {
        "grainId": "grain3",
        "nextGrainId": null,
        "extendedData": {
          "taskType": "dataOutput",
          "timeout": 15
        }
      }
    ]
  }
}
```

**Response Example**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "agentType": "Aevatar.GAgents.GroupChat.WorkflowCoordinator.WorkflowCoordinatorGAgent",
  "name": "Data Processing Workflow",
  "properties": {
    "workflowUnitList": [
      {
        "grainId": "grain1",
        "nextGrainId": "grain2",
        "extendedData": {
          "taskType": "dataValidation",
          "timeout": 30
        }
      }
    ]
  },
  "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
  "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
  "propertyJsonSchema": "{...}",
  "businessAgentGrainId": "business_agent_123"
}
```

### 1.4 Get Agent Details
**Method**: GET
**URL**: `/{guid}`

**Description**: Retrieve detailed information of a specific agent by agent ID, including configuration parameters and running status.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |

**Authorization**: Required

**Request Example**: `GET /api/agent/123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "agentType": "OpenAIAgent",
  "name": "My AI Assistant",
  "properties": {
    "apiKey": "sk-proj-xxxxxxxxxxxx",
    "model": "gpt-4",
    "temperature": 0.7
  },
  "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
  "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
  "propertyJsonSchema": "{...}",
  "businessAgentGrainId": "business_agent_123"
}
```

### 1.5 Get Agent Relationships
**Method**: GET
**URL**: `/{guid}/relationship`

**Description**: Retrieve the relationship graph of the specified agent, including hierarchical relationship information such as parent agents and child agents.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |

**Authorization**: Required

**Request Example**: `GET /api/agent/123e4567-e89b-12d3-a456-426614174000/relationship`

**Response Example**:
```json
{
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "parentAgents": [
    {
      "id": "parent-agent-id",
      "name": "Parent Agent",
      "agentType": "ManagerAgent"
    }
  ],
  "subAgents": [
    {
      "id": "sub-agent-id-1",
      "name": "Sub Agent 1",
      "agentType": "WorkerAgent"
    }
  ]
}
```

### 1.6 Send Message to Agent
**Method**: POST
**URL**: `/{guid}/send-message`

**Description**: Send a message to the specified agent for processing and return the agent's response.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |
| message | string | ✅ | Message content to send |
| conversationId | string? | ❌ | Conversation ID for context tracking |

**Authorization**: Required

**Request Example**:
```json
{
  "message": "Hello, please help me analyze this data",
  "conversationId": "conv-123"
}
```

**Response Example**:
```json
{
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "response": "I'd be happy to help you analyze the data. Please provide the data you'd like me to examine.",
  "conversationId": "conv-123",
  "timestamp": "2025-01-29T15:30:00Z"
}
```

### 1.7 Get Agent Conversation History
**Method**: GET
**URL**: `/{guid}/conversations`

**Description**: Retrieve conversation history for the specified agent.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |
| conversationId | string? | ❌ | Specific conversation ID |
| pageIndex | int | ❌ | Page index, default 0 |
| pageSize | int | ❌ | Page size, default 20 |

**Authorization**: Required

**Request Example**: `GET /api/agent/123e4567-e89b-12d3-a456-426614174000/conversations?pageIndex=0&pageSize=10`

**Response Example**:
```json
{
  "conversations": [
    {
      "conversationId": "conv-123",
      "messages": [
        {
          "role": "user",
          "content": "Hello, please help me analyze this data",
          "timestamp": "2025-01-29T15:30:00Z"
        },
        {
          "role": "agent",
          "content": "I'd be happy to help you analyze the data...",
          "timestamp": "2025-01-29T15:30:05Z"
        }
      ]
    }
  ],
  "totalCount": 1
}
```

### 1.8 Update Agent Configuration
**Method**: PUT
**URL**: `/{guid}`

**Description**: Update configuration parameters of the specified agent.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |
| name | string? | ❌ | New agent name |
| properties | Dictionary<string, object>? | ❌ | Updated configuration parameters |

**Authorization**: Required

**Request Example (OpenAI Agent)**:
```json
{
  "name": "Updated AI Assistant",
  "properties": {
    "model": "gpt-4-turbo",
    "temperature": 0.8
  }
}
```

**Request Example (Workflow Coordinator - Update Workflow Units)**:
```json
{
  "name": "Updated Data Processing Workflow",
  "properties": {
    "workflowUnitList": [
      {
        "grainId": "grain1",
        "nextGrainId": "grain2",
        "extendedData": {
          "taskType": "enhancedValidation",
          "timeout": 45
        }
      },
      {
        "grainId": "grain2",
        "nextGrainId": "grain4",
        "extendedData": {
          "taskType": "advancedTransformation",
          "timeout": 90
        }
      },
      {
        "grainId": "grain4",
        "nextGrainId": null,
        "extendedData": {
          "taskType": "finalOutput",
          "timeout": 20
        }
      }
    ]
  }
}
```

**Response Example**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "agentType": "Aevatar.GAgents.GroupChat.WorkflowCoordinator.WorkflowCoordinatorGAgent",
  "name": "Updated Data Processing Workflow",
  "properties": {
    "workflowUnitList": [
      {
        "grainId": "grain1",
        "nextGrainId": "grain2",
        "extendedData": {
          "taskType": "enhancedValidation",
          "timeout": 45
        }
      }
    ]
  },
  "updatedAt": "2025-01-29T15:45:00Z"
}
```

### 1.9 Delete Agent
**Method**: DELETE
**URL**: `/{guid}`

**Description**: Delete the specified agent instance.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |

**Authorization**: Required

**Request Example**: `DELETE /api/agent/123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "success": true,
  "message": "Agent deleted successfully"
}
```

### 1.10 Get Agent Status
**Method**: GET
**URL**: `/{guid}/status`

**Description**: Retrieve the current status and health information of the specified agent.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |

**Authorization**: Required

**Request Example**: `GET /api/agent/123e4567-e89b-12d3-a456-426614174000/status`

**Response Example**:
```json
{
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Active",
  "health": "Healthy",
  "lastActivity": "2025-01-29T15:30:00Z",
  "messageCount": 156,
  "uptime": "2d 14h 32m"
}
```

### 1.11 Restart Agent
**Method**: POST
**URL**: `/{guid}/restart`

**Description**: Restart the specified agent instance.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Agent unique identifier |

**Authorization**: Required

**Request Example**: `POST /api/agent/123e4567-e89b-12d3-a456-426614174000/restart`

**Response Example**:
```json
{
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Restarting",
  "message": "Agent restart initiated successfully",
  "timestamp": "2025-01-29T15:45:00Z"
}
```

### 1.12 Publish Event to Agent
**Method**: POST
**URL**: `/publishEvent`

**Description**: Publish an event to trigger agent actions, such as starting a workflow coordinator.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| agentId | Guid | ✅ | Target agent unique identifier |
| eventType | string | ✅ | Event type to publish |
| eventProperties | Dictionary<string, object>? | ❌ | Event-specific properties |

**Authorization**: Required

**Request Example (Start Workflow Coordinator)**:
```json
{
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "eventType": "Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent.StartWorkflowCoordinatorEvent",
  "eventProperties": {
    "initContent": "Process customer data batch #12345"
  }
}
```

**Request Example (Custom Event)**:
```json
{
  "agentId": "456e7890-e89b-12d3-a456-426614174000",
  "eventType": "CustomAgent.Events.ProcessDataEvent",
  "eventProperties": {
    "dataSource": "database",
    "batchSize": 1000,
    "priority": "high"
  }
}
```

**Response Example**:
```json
{
  "eventId": "event-789f0123-e89b-12d3-a456-426614174000",
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "eventType": "Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent.StartWorkflowCoordinatorEvent",
  "status": "Published",
  "publishedAt": "2025-01-29T16:00:00Z",
  "message": "Event successfully published to agent"
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 2. Plugin Management Module

### Base Path: `/api/plugins`

The Plugin Management system enables hot-pluggable extensions through WebHook.Host, supporting:

- **Dynamic DLL Loading**: Remote injection and loading of plugin assemblies without system restart
- **Polymorphic Discovery**: Automatic discovery and registration of IWebhookHandler implementations
- **Multi-tenant Isolation**: Secure plugin execution with tenant-specific contexts
- **Hot-Plug Architecture**: Add, update, or remove plugins without affecting system availability

### 2.1 Upload Plugin Package
**Method**: POST
**URL**: `/upload`

**Description**: Upload a plugin code package to the system. Supports ZIP format files up to 15MB.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| file | IFormFile | ✅ | Plugin package file (ZIP format, max 15MB) |
| name | string | ✅ | Plugin name |
| description | string? | ❌ | Plugin description |
| version | string? | ❌ | Plugin version |

**Authorization**: Required

**Request Example**: `POST /api/plugins/upload` (multipart/form-data)
```
Content-Type: multipart/form-data
file: [plugin.zip]
name: "My Custom Plugin"
description: "A powerful automation plugin"
version: "1.0.0"
```

**Response Example**:
```json
{
  "id": "plugin-123e4567-e89b-12d3-a456-426614174000",
  "name": "My Custom Plugin",
  "description": "A powerful automation plugin",
  "version": "1.0.0",
  "fileName": "plugin.zip",
  "fileSize": 1048576,
  "uploadedAt": "2025-01-29T15:30:00Z",
  "status": "Uploaded"
}
```

### 2.2 Get Plugin List
**Method**: GET
**URL**: `/`

**Description**: Retrieve a paginated list of uploaded plugins with filtering and sorting options.

**Request Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | int | ❌ | 0 | Page index, starting from 0 |
| pageSize | int | ❌ | 20 | Page size, maximum 100 |
| name | string? | ❌ | - | Filter by plugin name |
| status | string? | ❌ | - | Filter by status (Uploaded, Active, Inactive) |

**Authorization**: Required

**Request Example**: `GET /api/plugins?pageIndex=0&pageSize=10&status=Active`

**Response Example**:
```json
{
  "items": [
    {
      "id": "plugin-123e4567-e89b-12d3-a456-426614174000",
      "name": "My Custom Plugin",
      "description": "A powerful automation plugin",
      "version": "1.0.0",
      "status": "Active",
      "uploadedAt": "2025-01-29T15:30:00Z",
      "lastUsed": "2025-01-29T16:45:00Z"
    }
  ],
  "totalCount": 1,
  "pageIndex": 0,
  "pageSize": 10
}
```

### 2.3 Get Plugin Details
**Method**: GET
**URL**: `/{guid}`

**Description**: Retrieve detailed information about a specific plugin.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Plugin unique identifier |

**Authorization**: Required

**Request Example**: `GET /api/plugins/plugin-123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "id": "plugin-123e4567-e89b-12d3-a456-426614174000",
  "name": "My Custom Plugin",
  "description": "A powerful automation plugin",
  "version": "1.0.0",
  "fileName": "plugin.zip",
  "fileSize": 1048576,
  "status": "Active",
  "uploadedAt": "2025-01-29T15:30:00Z",
  "lastUsed": "2025-01-29T16:45:00Z",
  "metadata": {
    "author": "Developer Name",
    "dependencies": ["dependency1", "dependency2"],
    "entryPoint": "main.py"
  }
}
```

### 2.4 Delete Plugin
**Method**: DELETE
**URL**: `/{guid}`

**Description**: Delete a plugin package from the system.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Plugin unique identifier |

**Authorization**: Required

**Request Example**: `DELETE /api/plugins/plugin-123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "success": true,
  "message": "Plugin deleted successfully"
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 3. Notification Management Module

### Base Path: `/api/notification`

The Notification Management system provides event-driven messaging capabilities integrated with the Orleans cluster:

- **Multi-Channel Support**: Email, SMS, Push notifications, and WebSocket real-time messaging
- **Event-Driven Architecture**: Integrates with Kafka event streaming for reliable message delivery
- **SignalR Integration**: Real-time notifications through SignalR connections to frontend clients
- **Template System**: Reusable notification templates with variable substitution
- **Delivery Tracking**: Comprehensive delivery status tracking and retry mechanisms

### 3.1 Send Notification
**Method**: POST
**URL**: `/send`

**Description**: Send a notification message to specified recipients through various channels.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| title | string | ✅ | Notification title |
| message | string | ✅ | Notification content |
| recipients | string[] | ✅ | List of recipient identifiers |
| channel | string | ✅ | Notification channel (Email, SMS, Push, WebSocket) |
| priority | string? | ❌ | Priority level (Low, Normal, High, Critical) |
| scheduledAt | DateTime? | ❌ | Scheduled delivery time |

**Authorization**: Required

**Request Example**:
```json
{
  "title": "System Maintenance Notice",
  "message": "The system will undergo maintenance from 2:00 AM to 4:00 AM tomorrow.",
  "recipients": ["user123", "user456"],
  "channel": "Email",
  "priority": "High",
  "scheduledAt": "2025-01-30T02:00:00Z"
}
```

**Response Example**:
```json
{
  "notificationId": "notif-123e4567-e89b-12d3-a456-426614174000",
  "status": "Scheduled",
  "recipientCount": 2,
  "scheduledAt": "2025-01-30T02:00:00Z",
  "createdAt": "2025-01-29T15:30:00Z"
}
```

### 3.2 Get Notification List
**Method**: GET
**URL**: `/`

**Description**: Retrieve a paginated list of notifications with filtering options.

**Request Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | int | ❌ | 0 | Page index, starting from 0 |
| pageSize | int | ❌ | 20 | Page size, maximum 100 |
| status | string? | ❌ | - | Filter by status (Pending, Sent, Failed, Scheduled) |
| channel | string? | ❌ | - | Filter by channel |
| startDate | DateTime? | ❌ | - | Filter by start date |
| endDate | DateTime? | ❌ | - | Filter by end date |

**Authorization**: Required

**Request Example**: `GET /api/notification?pageIndex=0&pageSize=10&status=Sent&channel=Email`

**Response Example**:
```json
{
  "items": [
    {
      "id": "notif-123e4567-e89b-12d3-a456-426614174000",
      "title": "System Maintenance Notice",
      "channel": "Email",
      "status": "Sent",
      "recipientCount": 2,
      "createdAt": "2025-01-29T15:30:00Z",
      "sentAt": "2025-01-30T02:00:00Z"
    }
  ],
  "totalCount": 1,
  "pageIndex": 0,
  "pageSize": 10
}
```

### 3.3 Get Notification Details
**Method**: GET
**URL**: `/{guid}`

**Description**: Retrieve detailed information about a specific notification.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Notification unique identifier |

**Authorization**: Required

**Request Example**: `GET /api/notification/notif-123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "id": "notif-123e4567-e89b-12d3-a456-426614174000",
  "title": "System Maintenance Notice",
  "message": "The system will undergo maintenance from 2:00 AM to 4:00 AM tomorrow.",
  "channel": "Email",
  "priority": "High",
  "status": "Sent",
  "recipients": ["user123", "user456"],
  "createdAt": "2025-01-29T15:30:00Z",
  "scheduledAt": "2025-01-30T02:00:00Z",
  "sentAt": "2025-01-30T02:00:00Z",
  "deliveryReport": {
    "totalSent": 2,
    "successful": 2,
    "failed": 0
  }
}
```

### 3.4 Cancel Scheduled Notification
**Method**: POST
**URL**: `/{guid}/cancel`

**Description**: Cancel a scheduled notification that hasn't been sent yet.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Notification unique identifier |

**Authorization**: Required

**Request Example**: `POST /api/notification/notif-123e4567-e89b-12d3-a456-426614174000/cancel`

**Response Example**:
```json
{
  "notificationId": "notif-123e4567-e89b-12d3-a456-426614174000",
  "status": "Cancelled",
  "cancelledAt": "2025-01-29T16:00:00Z",
  "message": "Notification cancelled successfully"
}
```

### 3.5 Resend Failed Notification
**Method**: POST
**URL**: `/{guid}/resend`

**Description**: Resend a failed notification to its original recipients.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | Notification unique identifier |

**Authorization**: Required

**Request Example**: `POST /api/notification/notif-123e4567-e89b-12d3-a456-426614174000/resend`

**Response Example**:
```json
{
  "notificationId": "notif-123e4567-e89b-12d3-a456-426614174000",
  "status": "Resent",
  "resentAt": "2025-01-29T16:15:00Z",
  "recipientCount": 2
}
```

### 3.6 Get Notification Templates
**Method**: GET
**URL**: `/templates`

**Description**: Retrieve available notification templates for different types of messages.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| category | string? | ❌ | Filter by template category |
| channel | string? | ❌ | Filter by channel |

**Authorization**: Required

**Request Example**: `GET /api/notification/templates?category=System&channel=Email`

**Response Example**:
```json
[
  {
    "id": "template-maintenance",
    "name": "System Maintenance",
    "category": "System",
    "channel": "Email",
    "title": "System Maintenance Notice",
    "template": "The system will undergo maintenance from {{startTime}} to {{endTime}}.",
    "variables": ["startTime", "endTime"]
  }
]
```

### 3.7 Create Notification Template
**Method**: POST
**URL**: `/templates`

**Description**: Create a new notification template for reuse.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| name | string | ✅ | Template name |
| category | string | ✅ | Template category |
| channel | string | ✅ | Target channel |
| title | string | ✅ | Template title |
| template | string | ✅ | Template content with variables |
| variables | string[]? | ❌ | List of template variables |

**Authorization**: Required

**Request Example**:
```json
{
  "name": "Welcome Message",
  "category": "User",
  "channel": "Email",
  "title": "Welcome to {{platformName}}",
  "template": "Hello {{userName}}, welcome to {{platformName}}! Your account has been created successfully.",
  "variables": ["platformName", "userName"]
}
```

**Response Example**:
```json
{
  "id": "template-welcome",
  "name": "Welcome Message",
  "category": "User",
  "channel": "Email",
  "title": "Welcome to {{platformName}}",
  "template": "Hello {{userName}}, welcome to {{platformName}}! Your account has been created successfully.",
  "variables": ["platformName", "userName"],
  "createdAt": "2025-01-29T16:30:00Z"
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 4. Webhook Management Module

### Base Path: `/api/admin`

The Webhook Management system provides dynamic code deployment and execution capabilities:

- **Multi-DLL Loading**: Supports loading multiple DLL assemblies for complex webhook scenarios
- **Runtime Environment Support**: Compatible with Node.js, Python, .NET, and other runtime environments
- **Dynamic Deployment**: Deploy and update webhook code without system downtime
- **Environment Isolation**: Separate deployment environments (Development, Staging, Production) with specific configurations
- **Health Monitoring**: Real-time health checks and performance monitoring for deployed webhooks

### 4.1 Upload Code Package
**Method**: POST
**URL**: `/upload-code`

**Description**: Upload a code package for webhook deployment. Supports various archive formats up to 200MB.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| file | IFormFile | ✅ | Code package file (ZIP, TAR, etc., max 200MB) |
| name | string | ✅ | Package name |
| version | string? | ❌ | Package version |
| description | string? | ❌ | Package description |
| runtime | string? | ❌ | Runtime environment (Node.js, Python, .NET, etc.) |

**Authorization**: AdminPolicy

**Request Example**: `POST /api/admin/upload-code` (multipart/form-data)
```
Content-Type: multipart/form-data
file: [webhook-code.zip]
name: "Data Processing Webhook"
version: "2.1.0"
description: "Webhook for processing incoming data"
runtime: "Node.js"
```

**Response Example**:
```json
{
  "packageId": "pkg-123e4567-e89b-12d3-a456-426614174000",
  "name": "Data Processing Webhook",
  "version": "2.1.0",
  "fileName": "webhook-code.zip",
  "fileSize": 52428800,
  "runtime": "Node.js",
  "uploadedAt": "2025-01-29T15:30:00Z",
  "status": "Uploaded"
}
```

### 4.2 Get Code Package List
**Method**: GET
**URL**: `/code-packages`

**Description**: Retrieve a list of uploaded code packages with filtering and pagination.

**Request Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | int | ❌ | 0 | Page index, starting from 0 |
| pageSize | int | ❌ | 20 | Page size, maximum 100 |
| name | string? | ❌ | - | Filter by package name |
| runtime | string? | ❌ | - | Filter by runtime |
| status | string? | ❌ | - | Filter by status |

**Authorization**: AdminPolicy

**Request Example**: `GET /api/admin/code-packages?pageIndex=0&pageSize=10&runtime=Node.js`

**Response Example**:
```json
{
  "items": [
    {
      "packageId": "pkg-123e4567-e89b-12d3-a456-426614174000",
      "name": "Data Processing Webhook",
      "version": "2.1.0",
      "runtime": "Node.js",
      "status": "Active",
      "uploadedAt": "2025-01-29T15:30:00Z",
      "lastDeployed": "2025-01-29T16:00:00Z"
    }
  ],
  "totalCount": 1,
  "pageIndex": 0,
  "pageSize": 10
}
```

### 4.3 Deploy Code Package
**Method**: POST
**URL**: `/{packageId}/deploy`

**Description**: Deploy a code package to the webhook execution environment.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| packageId | Guid | ✅ | Code package identifier |
| environment | string | ✅ | Deployment environment (Development, Staging, Production) |
| configuration | Dictionary<string, object>? | ❌ | Environment-specific configuration |

**Authorization**: AdminPolicy

**Request Example**:
```json
{
  "environment": "Production",
  "configuration": {
    "maxMemory": "512MB",
    "timeout": 30,
    "environmentVariables": {
      "NODE_ENV": "production",
      "LOG_LEVEL": "info"
    }
  }
}
```

**Response Example**:
```json
{
  "deploymentId": "deploy-123e4567-e89b-12d3-a456-426614174000",
  "packageId": "pkg-123e4567-e89b-12d3-a456-426614174000",
  "environment": "Production",
  "status": "Deploying",
  "deployedAt": "2025-01-29T16:00:00Z",
  "webhookUrl": "https://api.aevatarstation.com/webhook/deploy-123e4567-e89b-12d3-a456-426614174000"
}
```

### 4.4 Get Deployment Status
**Method**: GET
**URL**: `/deployments/{deploymentId}`

**Description**: Check the status of a webhook deployment.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| deploymentId | Guid | ✅ | Deployment identifier |

**Authorization**: AdminPolicy

**Request Example**: `GET /api/admin/deployments/deploy-123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "deploymentId": "deploy-123e4567-e89b-12d3-a456-426614174000",
  "packageId": "pkg-123e4567-e89b-12d3-a456-426614174000",
  "environment": "Production",
  "status": "Active",
  "deployedAt": "2025-01-29T16:00:00Z",
  "webhookUrl": "https://api.aevatarstation.com/webhook/deploy-123e4567-e89b-12d3-a456-426614174000",
  "health": {
    "status": "Healthy",
    "lastCheck": "2025-01-29T16:30:00Z",
    "responseTime": 45
  },
  "logs": [
    {
      "timestamp": "2025-01-29T16:00:00Z",
      "level": "Info",
      "message": "Webhook deployed successfully"
    }
  ]
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 5. Subscription Management Module

### Base Path: `/api/subscription`

The Subscription Management system enables event-driven communication through Orleans Streams and Kafka:

- **Event Stream Integration**: Leverages Orleans Streams with Kafka Stream Provider for reliable event delivery
- **Agent Event Subscription**: Subscribe to specific agent events and state changes
- **Webhook Callbacks**: Automatic webhook notifications when subscribed events occur
- **Filtering Capabilities**: Advanced event filtering based on agent types, priorities, and custom criteria
- **Stream Reliability**: Built-in retry mechanisms and delivery guarantees through Orleans Streams

### 5.1 Create Subscription
**Method**: POST
**URL**: `/`

**Description**: Create a new event subscription to receive notifications when specific events occur.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| eventType | string | ✅ | Type of event to subscribe to |
| callbackUrl | string | ✅ | URL to receive webhook notifications |
| filters | Dictionary<string, object>? | ❌ | Event filtering criteria |
| isActive | bool | ❌ | Whether subscription is active (default: true) |

**Authorization**: SubscriptionManagement.CreateSubscription

**Request Example**:
```json
{
  "eventType": "agent.message.received",
  "callbackUrl": "https://myapp.com/webhooks/agent-messages",
  "filters": {
    "agentType": "OpenAIAgent",
    "priority": "High"
  },
  "isActive": true
}
```

**Response Example**:
```json
{
  "subscriptionId": "sub-123e4567-e89b-12d3-a456-426614174000",
  "eventType": "agent.message.received",
  "callbackUrl": "https://myapp.com/webhooks/agent-messages",
  "filters": {
    "agentType": "OpenAIAgent",
    "priority": "High"
  },
  "isActive": true,
  "createdAt": "2025-01-29T15:30:00Z",
  "secretKey": "sub_secret_abcd1234efgh5678"
}
```

### 5.2 Get Subscription List
**Method**: GET
**URL**: `/`

**Description**: Retrieve a list of event subscriptions with filtering and pagination.

**Request Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | int | ❌ | 0 | Page index, starting from 0 |
| pageSize | int | ❌ | 20 | Page size, maximum 100 |
| eventType | string? | ❌ | - | Filter by event type |
| isActive | bool? | ❌ | - | Filter by active status |

**Authorization**: SubscriptionManagement.ViewSubscriptionStatus

**Request Example**: `GET /api/subscription?pageIndex=0&pageSize=10&eventType=agent.message.received`

**Response Example**:
```json
{
  "items": [
    {
      "subscriptionId": "sub-123e4567-e89b-12d3-a456-426614174000",
      "eventType": "agent.message.received",
      "callbackUrl": "https://myapp.com/webhooks/agent-messages",
      "isActive": true,
      "createdAt": "2025-01-29T15:30:00Z",
      "lastTriggered": "2025-01-29T16:45:00Z",
      "triggerCount": 25
    }
  ],
  "totalCount": 1,
  "pageIndex": 0,
  "pageSize": 10
}
```

### 5.3 Update Subscription
**Method**: PUT
**URL**: `/{subscriptionId}`

**Description**: Update an existing event subscription configuration.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| subscriptionId | Guid | ✅ | Subscription identifier |
| callbackUrl | string? | ❌ | New callback URL |
| filters | Dictionary<string, object>? | ❌ | Updated filtering criteria |
| isActive | bool? | ❌ | Updated active status |

**Authorization**: SubscriptionManagement.CreateSubscription

**Request Example**:
```json
{
  "callbackUrl": "https://myapp.com/webhooks/updated-endpoint",
  "filters": {
    "agentType": "OpenAIAgent",
    "priority": ["High", "Critical"]
  },
  "isActive": true
}
```

**Response Example**:
```json
{
  "subscriptionId": "sub-123e4567-e89b-12d3-a456-426614174000",
  "eventType": "agent.message.received",
  "callbackUrl": "https://myapp.com/webhooks/updated-endpoint",
  "filters": {
    "agentType": "OpenAIAgent",
    "priority": ["High", "Critical"]
  },
  "isActive": true,
  "updatedAt": "2025-01-29T16:30:00Z"
}
```

### 5.4 Cancel Subscription
**Method**: DELETE
**URL**: `/{subscriptionId}`

**Description**: Cancel and delete an event subscription.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| subscriptionId | Guid | ✅ | Subscription identifier |

**Authorization**: SubscriptionManagement.CancelSubscription

**Request Example**: `DELETE /api/subscription/sub-123e4567-e89b-12d3-a456-426614174000`

**Response Example**:
```json
{
  "subscriptionId": "sub-123e4567-e89b-12d3-a456-426614174000",
  "status": "Cancelled",
  "cancelledAt": "2025-01-29T17:00:00Z",
  "message": "Subscription cancelled successfully"
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 6. API Key Management Module

### Base Path: `/api/appId`

### 6.1 Create API Key
**Method**: POST
**URL**: `/`

**Description**: Create a new API key for accessing the platform APIs.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| appName | string | ✅ | API key name/description |
| projectId | Guid | ✅ | Associated project ID |
| permissions | string[]? | ❌ | List of permissions for this key |
| expiresAt | DateTime? | ❌ | Expiration date (optional) |

**Authorization**: ApiKeys.Create

**Request Example**:
```json
{
  "appName": "Production API Key",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "permissions": ["agent.read", "agent.write", "notification.send"],
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

**Response Example**:
```json
{
  "id": "appid-123",
  "name": "Production API Key",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "apiKey": "ak-prod-1234567890abcdef1234567890abcdef",
  "permissions": ["agent.read", "agent.write", "notification.send"],
  "createdAt": "2025-01-29T15:30:00Z",
  "expiresAt": "2025-12-31T23:59:59Z",
  "status": "Active"
}
```

### 6.2 Get API Key List
**Method**: GET
**URL**: `/`

**Description**: Retrieve a list of API keys for the current user or project.

**Request Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| projectId | Guid? | ❌ | - | Filter by project ID |
| pageIndex | int | ❌ | 0 | Page index, starting from 0 |
| pageSize | int | ❌ | 20 | Page size, maximum 100 |

**Authorization**: ApiKeys.Default

**Request Example**: `GET /api/appId?projectId=123e4567-e89b-12d3-a456-426614174000&pageIndex=0&pageSize=10`

**Response Example**:
```json
[
  {
    "id": "appid-123",
    "name": "Production API Key",
    "projectId": "123e4567-e89b-12d3-a456-426614174000",
    "keyPreview": "ak-prod-xxxx...xxxx",
    "createdAt": "2025-01-29T10:30:00Z",
    "lastUsedAt": "2025-01-29T14:20:00Z",
    "status": "Active"
  },
  {
    "id": "appid-456",
    "name": "Testing API Key",
    "projectId": "123e4567-e89b-12d3-a456-426614174000",
    "keyPreview": "ak-test-xxxx...xxxx",
    "createdAt": "2025-01-28T16:15:00Z",
    "lastUsedAt": null,
    "status": "Active"
  }
]
```

### 6.3 Delete API Key
**Method**: DELETE
**URL**: `/{guid}`

**Description**: Delete an API key, making it unusable for API access.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | API key identifier |

**Authorization**: Required

**Request Example**: `DELETE /api/appId/appid-456`

**Response Example**:
```json
{
  "success": true,
  "message": "API key deleted successfully"
}
```

### 6.4 Update API Key Name
**Method**: PUT
**URL**: `/{guid}`

**Description**: Update the display name of an API key without affecting the key value itself.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| guid | Guid | ✅ | API key identifier |
| appName | string | ✅ | New name |

**Authorization**: Required

**Request Example**:
```json
{
  "appName": "Updated API Key Name"
}
```

**Response Example**:
```json
{
  "id": "appid-123",
  "name": "Updated API Key Name",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "updatedAt": "2025-01-29T15:30:00Z"
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 7. API Request Statistics Module

### Base Path: `/api/api-requests`

The API Request Statistics system provides comprehensive monitoring and analytics for the distributed Orleans cluster:

- **Multi-Silo Monitoring**: Aggregates statistics across Scheduler, Projector, and User silos
- **Real-time Metrics**: Integration with Prometheus for real-time performance monitoring
- **Distributed Tracing**: Jaeger integration for request tracing across Orleans grains
- **Performance Analytics**: Response time analysis, error rate tracking, and throughput metrics
- **Grafana Dashboards**: Pre-configured dashboards for visualizing API usage patterns and system health

### 7.1 Get API Request Statistics
**Method**: GET
**URL**: `/`

**Description**: Retrieve API request statistics, supporting viewing API usage by organization or project dimension.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| organizationId | Guid? | Conditional | Organization ID, choose one with projectId |
| projectId | Guid? | Conditional | Project ID, choose one with organizationId |
| startDate | DateTime? | ❌ | Statistics start time |
| endDate | DateTime? | ❌ | Statistics end time |

**Authorization**: ApiRequests.Default

**Request Example**: `GET /api/api-requests?projectId=123e4567-e89b-12d3-a456-426614174000&startDate=2025-01-01&endDate=2025-01-31`

**Response Example**:
```json
{
  "totalRequests": 1250,
  "requests": [
    {
      "endpoint": "/api/agent",
      "method": "POST",
      "count": 450,
      "avgResponseTime": 125.5,
      "errorRate": 0.02
    },
    {
      "endpoint": "/api/plugins",
      "method": "GET",
      "count": 380,
      "avgResponseTime": 88.2,
      "errorRate": 0.01
    },
    {
      "endpoint": "/api/notification",
      "method": "POST",
      "count": 420,
      "avgResponseTime": 156.8,
      "errorRate": 0.03
    }
  ],
  "timeRange": {
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": "2025-01-31T23:59:59Z"
  }
}
```

---

[⬆️ Back to Table of Contents](#table-of-contents)

## 8. Query Service Module

### Base Path: `/api/query`

The Query Service module provides advanced search capabilities through ElasticSearch integration:

- **ElasticSearch Integration**: Direct access to indexed agent states and workflow data
- **State Querying**: Query agent states including WorkflowCoordinatorState
- **Real-time Search**: Live search across distributed Orleans grain states
- **Flexible Filtering**: Support for complex queries and filtering criteria

### 8.1 Query ElasticSearch
**Method**: GET
**URL**: `/es`

**Description**: Query ElasticSearch for agent states, workflow information, and other indexed data.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| state | string? | ❌ | State type to query (e.g., WorkflowCoordinatorState) |
| agentId | Guid? | ❌ | Filter by specific agent ID |
| query | string? | ❌ | ElasticSearch query string |
| from | int | ❌ | Starting index for pagination (default: 0) |
| size | int | ❌ | Number of results to return (default: 10) |

**Authorization**: Required

**Request Example (Query Workflow States)**:
`GET /api/query/es?state=WorkflowCoordinatorState&agentId=123e4567-e89b-12d3-a456-426614174000&size=20`

**Request Example (General Query)**:
`GET /api/query/es?query=status:active AND agentType:WorkflowCoordinator&from=0&size=50`

**Response Example (Workflow Coordinator State)**:
```json
{
  "items": [
    {
      "agentId": "123e4567-e89b-12d3-a456-426614174000",
      "state": "WorkflowCoordinatorState",
      "currentWorkUnitInfos": "{\"activeUnit\":\"grain2\",\"status\":\"processing\",\"startTime\":\"2025-01-29T16:00:00Z\",\"currentStep\":2,\"totalSteps\":3}",
      "workflowStatus": "Running",
      "lastUpdated": "2025-01-29T16:15:00Z",
      "metadata": {
        "workflowId": "wf-12345",
        "initiatedBy": "user123",
        "priority": "high"
      }
    },
    {
      "agentId": "456e7890-e89b-12d3-a456-426614174000",
      "state": "WorkflowCoordinatorState",
      "currentWorkUnitInfos": "{\"activeUnit\":\"grain1\",\"status\":\"completed\",\"completedTime\":\"2025-01-29T15:45:00Z\",\"currentStep\":3,\"totalSteps\":3}",
      "workflowStatus": "Completed",
      "lastUpdated": "2025-01-29T15:45:00Z",
      "metadata": {
        "workflowId": "wf-12346",
        "initiatedBy": "user456",
        "priority": "normal"
      }
    }
  ],
  "totalCount": 2,
  "from": 0,
  "size": 20
}
```

**Workflow Unit Info JSON Structure**:
```json
{
  "activeUnit": "grain2",
  "status": "processing",
  "startTime": "2025-01-29T16:00:00Z",
  "currentStep": 2,
  "totalSteps": 3,
  "completedUnits": ["grain1"],
  "pendingUnits": ["grain3"],
  "errors": [],
  "executionContext": {
    "batchId": "batch-789",
    "retryCount": 0,
    "timeout": 300
  }
}
```

---

## Permission System

### Permission Levels
1. **AdminPolicy**: Administrator permissions
   - Full access to system management functions
2. **EventManagement**: Event management permissions
   - View: View events
3. **SubscriptionManagement**: Subscription management permissions
   - CreateSubscription: Create subscriptions
   - CancelSubscription: Cancel subscriptions
   - ViewSubscriptionStatus: View subscription status
4. **ApiKeys**: API key permissions
   - Default: View keys
   - Create: Create keys
5. **ApiRequests**: API request statistics permissions
   - Default: View statistics

### Authentication Method
- All APIs requiring permissions need to include `Authorization: Bearer {token}` in the request headers
- Some special permissions require additional role or permission verification

---

## Error Handling

### Common HTTP Status Codes
- **200**: Request successful
- **400**: Request parameter error
- **401**: Unauthorized
- **403**: Insufficient permissions
- **404**: Resource not found
- **413**: File too large
- **422**: Parameter validation failed
- **500**: Internal server error

### Error Response Format
```json
{
  "error": {
    "code": "ValidationError",
    "message": "Request parameter validation failed",
    "details": "AgentType is required",
    "validationErrors": [
      {
        "field": "agentType",
        "message": "Agent type cannot be empty"
      }
    ]
  }
}
```

---

## Version Information
- **Document Version**: v2.0
- **API Version**: v1
- **Update Date**: 2025-01-29
- **Generation Time**: Auto-generated based on code analysis

---

[⬆️ Back to Table of Contents](#table-of-contents)

*This document is automatically generated based on the controller code of the AevatarStation project, containing detailed API interface information for 7 core business modules. Each interface includes complete function descriptions, parameter definitions, request examples, and response formats.*
</rewritten_file>