# Aevatar.Domain

This project is the core domain layer for the Aevatar application, implementing the business logic and domain models.

## Overview

The Domain layer encapsulates the business rules, entities, and domain logic of the Aevatar application. It's built on top of the ABP Framework and follows domain-driven design principles.

## Project Structure

- **Agent/** - Agent-related domain entities and business logic
- **AgentPlugins/** - Plugin functionality for agents
- **AINameContest/** - AI naming contest feature domain objects
- **ApiKey/** - API key management domain logic
- **ApiRequests/** - API request handling domain models
- **Cqrs/** - Command Query Responsibility Segregation pattern implementations
- **Data/** - Data access and management
- **Host/** - Host-related domain logic
- **Notification/** - Notification system domain logic
- **OpenIddict/** - OpenIddict authentication integration
- **Permissions/** - Permission management for the application
- **Settings/** - Application settings domain models
- **Subscription/** - Subscription management domain objects
- **User/** - User domain entities and business logic
- **Webhook/** - Webhook processing domain logic

## Dependencies

The project depends on:

- **Aevatar.Domain.Shared** - Shared domain contracts and objects
- **Aevatar.Domain.Grains** - Domain grain interfaces for Orleans

## NuGet Packages

- **NJsonSchema** - JSON Schema generation and validation
- **AElf.OpenTelemetry** - Telemetry and monitoring
- **Volo.Abp packages:**
  - **Volo.Abp.Emailing** - Email service integration
  - **Volo.Abp.Identity.Domain** - Identity management
  - **Volo.Abp.PermissionManagement.Domain.Identity** - Permission management for identity
  - **Volo.Abp.BackgroundJobs.Domain** - Background job processing
  - **Volo.Abp.AuditLogging.Domain** - Audit logging functionality
  - **Volo.Abp.OpenIddict.Domain** - OpenID Connect & OAuth 2.0 implementation
  - **Volo.Abp.PermissionManagement.Domain.OpenIddict** - Permission management for OpenIddict

## Module Configuration

The `AevatarDomainModule` class serves as the entry point for the ABP module system. It:

1. Configures the domain layer services
2. Sets up supported languages and localization options
3. Integrates with the ABP framework ecosystem

## Constants

The `AevatarConsts` class defines important application constants:

- Database configuration (table prefixes, schema)
- Role names and permissions
- Security configuration
- Organization-related constants

## Development

This project targets .NET 9.0 and has nullable reference types enabled for improved type safety. 