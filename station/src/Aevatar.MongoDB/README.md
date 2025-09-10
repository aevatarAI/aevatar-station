# Aevatar.MongoDB

This project contains the MongoDB data access layer for the Aevatar application.

## Overview

`Aevatar.MongoDB` implements the repository interfaces defined in the domain layer using MongoDB as the persistence mechanism. It provides MongoDB-specific implementations for all repositories and configures the MongoDB mapping for domain entities.

## Key Features

- MongoDB repository implementations
- Entity mappings and configuration
- MongoDB-specific querying extensions
- Transactional operations support
- Index management
- Performance optimizations

## Dependencies

The project leverages the following key ABP MongoDB modules:

- `Volo.Abp.FeatureManagement.MongoDB`
- `Volo.Abp.SettingManagement.MongoDB`
- `Volo.Abp.PermissionManagement.MongoDB`
- `Volo.Abp.Identity.MongoDB`
- `Volo.Abp.BackgroundJobs.MongoDB`
- `Volo.Abp.AuditLogging.MongoDB`
- `Volo.Abp.OpenIddict.MongoDB`
- `Volo.Abp.TenantManagement.MongoDB`

It also references the following project:
- `Aevatar.Domain` - Domain layer containing entities and repository interfaces

## Database Schema

The MongoDB collections follow ABP Framework conventions, generally matching entity names in plural form. Each collection includes standard ABP auditing fields and uses MongoDB's document model to store related entities as embedded documents where appropriate.

## Connection Configuration

The MongoDB connection is configured in the host project's `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017/Aevatar",
    "DatabaseName": "Aevatar"
  }
}
```

## Project Structure

- **Collections/**: MongoDB collection configuration
- **Repositories/**: MongoDB implementations of domain repositories
- **MongoDb/**: Context and connection management

## Usage

This project should be referenced by any host application that needs data persistence. The MongoDB repositories are registered with the ABP dependency injection system and can be injected where needed.

## Performance Considerations

- Indexes are created for frequently queried fields
- Aggregate operations are used for complex queries
- Projection is used to limit returned fields for better performance
- Appropriate MongoDB drivers are selected based on operation type

## Logging

Repository operations log important database operations and errors at appropriate checkpoints to facilitate debugging and monitoring. 