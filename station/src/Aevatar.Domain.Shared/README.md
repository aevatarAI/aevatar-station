# Aevatar.Domain.Shared

This project contains the shared domain layer resources for the Aevatar application.

## Overview

`Aevatar.Domain.Shared` serves as a fundamental building block containing domain constants, enums, and other shared domain structures that are utilized across the solution. These resources establish consistency in the domain model and ensure proper abstraction of core business rules.

## Key Features

- Domain constants and enums
- Shared DTOs and resource models
- Localization resources
- Shared domain exceptions
- Cross-cutting domain validation rules

## Dependencies

The project leverages the following key dependencies:

- `Aevatar.Core.Abstractions` - Core abstractions for the Aevatar platform
- `Volo.Abp.Dapr` - ABP Framework Dapr integration
- `Volo.Abp.AspNetCore.Mvc.Dapr.EventBus` - Dapr event bus integration
- Various ABP domain shared modules (Identity, BackgroundJobs, AuditLogging, etc.)
- `Microsoft.Orleans.Sdk` - Orleans distributed computing framework SDK

## Localization

The project includes embedded JSON-based localization resources:

```
/Localization/Aevatar/*.json
```

## Project Structure

- **Constants/**: Domain-wide constant definitions
- **Enums/**: Shared enumeration types
- **Localization/**: Localization resources
- **ValueObjects/**: Shared value objects for domain entities

## Usage

This project should be referenced by any project that needs access to shared domain types, constants, or localization resources. It provides a central location for shared domain resources that need to be consistent across the system.

## Logging

Important domain events and errors are logged at appropriate checkpoints to facilitate debugging and monitoring. 