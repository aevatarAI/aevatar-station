# AevatarStation API Interface Design Document v2

## Overview

AevatarStation is a full-stack platform for developing, managing, and deploying AI agents, built on the core philosophy that "an agent is a self-referential structure of language." The platform employs distributed, event-driven, and pluggable design principles, supporting flexible extension and high-availability deployment.

## Table of Contents

- [Overview](#overview)
- [1. Workflow Management Module - 2 APIs](#1-workflow-management-module)
- [2. Agent System Module - 12 APIs](#2-agent-system-module)
- [3. Plugin Management Module - 4 APIs](#3-plugin-management-module)
- [4. Notification Management Module - 7 APIs](#4-notification-management-module)
- [5. Webhook Management Module - 4 APIs](#5-webhook-management-module)
- [6. Subscription Management Module - 4 APIs](#6-subscription-management-module)
- [7. API Key Management Module - 4 APIs](#7-api-key-management-module)
- [8. API Request Statistics Module - 1 API](#8-api-request-statistics-module)
- [9. Query Service Module - 1 API](#9-query-service-module)

---

## 1. Workflow Management Module

### Base Path: `/api/workflow`

The Workflow Management system provides AI-powered workflow generation and text completion capabilities:

- **AI Workflow Generation**: Automatically generate workflow configurations based on user goals
- **Text Completion**: Generate multiple text completion options for user input
- **Workflow Orchestration**: Coordinate and manage complex workflow execution
- **Real-time Updates**: Monitor workflow status and progress

### 1.1 Generate Workflow
**Method**: POST  
**URL**: `/generate`

**Description**: Generate a workflow configuration based on the user's goal description.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userGoal | string | ✅ | User's goal description for workflow generation |

**Authorization**: Required

**Request Example**:
```json
{
  "userGoal": "Process customer data and generate monthly reports"
}
```

**Response Example**:
```json
{
  "properties": {
    "workflowNodeList": [
      {
        "nodeId": "node1",
        "nodeType": "dataValidation",
        "nextNodeId": "node2",
        "configuration": {
          "validationRules": ["format", "completeness"],
          "timeout": 30
        }
      },
      {
        "nodeId": "node2",
        "nodeType": "dataProcessing",
        "nextNodeId": "node3",
        "configuration": {
          "processingType": "aggregation",
          "timeout": 60
        }
      },
      {
        "nodeId": "node3",
        "nodeType": "reportGeneration",
        "nextNodeId": null,
        "configuration": {
          "format": "pdf",
          "template": "monthly-report",
          "timeout": 45
        }
      }
    ]
  }
}
```

### 1.2 Generate Text Completions
**Method**: POST  
**URL**: `/text-completion/generate`

**Description**: Generate five different text completion options based on user input.

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userGoal | string | ✅ | User's input text for completion generation |

**Authorization**: Required

**Request Example**:
```json
{
  "userGoal": "Write a professional email to"
}
```

**Response Example**:
```json
{
  "completions": [
    {
      "text": "Write a professional email to schedule a client meeting",
      "confidence": 0.95
    },
    {
      "text": "Write a professional email to follow up on project status",
      "confidence": 0.92
    },
    {
      "text": "Write a professional email to request feedback on proposal",
      "confidence": 0.89
    },
    {
      "text": "Write a professional email to introduce new team member",
      "confidence": 0.87
    },
    {
      "text": "Write a professional email to confirm receipt of documents",
      "confidence": 0.85
    }
  ]
}
```

[Previous sections remain unchanged...]