# Aevatar API Reference

## Overview

Welcome to the Aevatar API documentation. This document provides a comprehensive guide to all available HTTP endpoints in the Aevatar platform.

## Authentication

Most API endpoints require authentication. Use the following authentication methods:
- Bearer Token Authentication
- API Key Authentication (for specific endpoints)

## API Categories

### Workflow Management
- [Workflow APIs](workflow/workflow.md) - Create, manage, and monitor workflows
- [Workflow View APIs](workflow/workflow-view.md) - View and analyze workflow data

### Agent Management
- [Agent APIs](agent/agent.md) - Agent lifecycle and configuration management
- [Host APIs](agent/host.md) - Host management and monitoring
- [Plugin APIs](agent/plugin.md) - Plugin management and configuration

### Organization Management
- [Organization APIs](organization/organization.md) - Organization CRUD operations
- [Organization Roles](organization/organization-role.md) - Role management within organizations
- [Organization Permissions](organization/organization-permission.md) - Permission management for organizations

### Project Management
- [Project APIs](project/project.md) - Project CRUD and configuration
- [Project Roles](project/project-role.md) - Role management within projects
- [Project Permissions](project/project-permission.md) - Permission management for projects
- [Project CORS](project/project-cors.md) - CORS configuration for projects

### Account and Authentication
- [Account APIs](account/account.md) - User account management
- [AppId APIs](account/appid.md) - Application ID management
- [API Request](account/api-request.md) - API request tracking and management

### Additional Features
- [Query APIs](other/query.md) - Data query operations
- [Subscription APIs](other/subscription.md) - Subscription management
- [Notification APIs](other/notification.md) - Notification system
- [Blob Storage APIs](other/blob-storage.md) - File storage operations
- [Developer APIs](other/developer.md) - Developer-specific operations

## Rate Limiting

API rate limits are applied based on:
- Authentication type
- Endpoint category
- User/Organization tier

## Common Response Codes

- 200: Successful operation
- 201: Resource created
- 400: Bad request
- 401: Unauthorized
- 403: Forbidden
- 404: Resource not found
- 429: Too many requests
- 500: Internal server error

## Support

For API support, please contact our developer support team or refer to the developer documentation.