# Aevatar.Silo

This project hosts the Orleans silo for the Aevatar distributed application platform.

## Overview

`Aevatar.Silo` is the Orleans hosting application that runs the server-side grains for the Aevatar distributed system. It provides a scalable, highly available runtime environment for Orleans grains, enabling distributed computing capabilities for the platform.

## Key Features

- Orleans silo host
- Grain activation and lifecycle management
- Distributed state management
- Reminders and timers
- Stream processing with Kafka
- Grain persistence with MongoDB
- Dashboard for monitoring and diagnostics
- Event sourcing support
- Semantic Kernel integration for AI capabilities

## Dependencies

The project leverages the following key dependencies:

- `Microsoft.Orleans.Server` - Orleans silo runtime
- `Microsoft.Orleans.Runtime` - Core Orleans functionality
- `Microsoft.Orleans.Reminders` - Scheduled reminders for grains
- `Orleans.Providers.MongoDB` - MongoDB persistence for Orleans
- `OrleansDashboard` - Runtime monitoring and diagnostics
- `Serilog` - Structured logging
- `AElf.OpenTelemetry` - Observability infrastructure
- `Orleans.Streams.Kafka` - Event streaming
- `Aevatar.EventSourcing` - Event sourcing capabilities
- `Aevatar.Plugins` - Extensibility system
- `Aevatar.GAgents.SemanticKernel` - AI integration

## Project Structure

- **Program.cs**: Entry point and silo configuration
- **Configuration/**: Silo configuration settings
- **Hosting/**: Host service configurations

## Referenced Projects

- `Aevatar.MongoDB` - Data persistence
- `Aevatar.Domain.Grains` - Domain-level grain implementations
- `Aevatar.Application.Grains` - Application-level grain implementations

## Grain Types

The silo hosts various grain types including:

- Stateless grains for simple processing
- Stateful grains for maintaining entity state
- Reminder-based grains for scheduled operations
- Event-sourced grains for event-driven architectures
- Stream-processing grains for real-time data analysis

## Configuration

The silo is configured through `appsettings.json`, which includes settings for:

- Orleans clustering
- Grain persistence
- Silo endpoint configuration
- Stream providers
- Reminder services
- Dashboard configuration
- Logging settings

## Scaling

The silo is designed to scale horizontally. Multiple instances can be deployed behind a load balancer to handle increased load, with Orleans handling the distributed coordination automatically.

## Monitoring

- Orleans Dashboard at `http://localhost:8080/dashboard`
- Structured logs via Serilog
- OpenTelemetry integration for distributed tracing

## Deployment

The silo can be deployed as a standalone service or as part of a Kubernetes deployment. In production, multiple silo instances should be deployed for high availability.

## Getting Started

1. Ensure MongoDB is running
2. Configure connection strings in `appsettings.json`
3. Run the silo using `dotnet run`
4. The silo will start and begin accepting grain activations

## Logging

The silo uses Serilog for structured logging, with output to:
- Console
- OpenTelemetry (for centralized observability) 