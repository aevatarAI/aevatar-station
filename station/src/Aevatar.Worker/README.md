# Aevatar.Worker

This project provides background processing capabilities for the Aevatar platform.

## Overview

`Aevatar.Worker` is a background processing service that handles long-running tasks, scheduled jobs, and event-driven operations that don't require immediate response to HTTP requests. It operates independently from the web API, communicating with other components through Orleans and event buses.

## Key Features

- Background job processing
- Scheduled task execution
- Event handling and processing
- Integration with Orleans distributed computing
- Dapr event bus integration
- MongoDB persistence
- Monitoring and health checks
- Scalable worker architecture

## Dependencies

The project leverages the following key dependencies:

- `Serilog` - Structured logging
- `Volo.Abp.Autofac` - Dependency injection
- `Volo.Abp.AspNetCore.Mvc` - MVC components
- `Volo.Abp.AspNetCore.Serilog` - Serilog integration
- `Orleans.Providers.MongoDB` - MongoDB provider for Orleans
- `Microsoft.Orleans.Client` - Orleans client for grain communication
- `AElf.OpenTelemetry` - Observability infrastructure
- `Volo.Abp.Dapr` - Dapr integration
- `Volo.Abp.AspNetCore.Mvc.Dapr.EventBus` - Dapr event bus

## Referenced Projects

- `Aevatar.Application` - Application services
- `Aevatar.Application.Contracts` - Contracts and DTOs
- `Aevatar.MongoDB` - Data persistence

## Worker Types

The worker supports different types of background operations:

- **Scheduled Jobs**: Tasks that run on a schedule (e.g., daily reports, cleanup operations)
- **Queue Processors**: Workers that consume messages from queues
- **Event Handlers**: Processors that react to domain events
- **Data Processors**: Long-running operations on large datasets
- **Integration Tasks**: Background operations that synchronize with external systems

## Configuration

The worker is configured through `appsettings.json`:

```json
{
  "App": {
    "Name": "Aevatar.Worker"
  },
  "ConnectionStrings": {
    "Default": "mongodb://localhost:27017/Aevatar"
  },
  "Orleans": {
    "ClusterId": "dev",
    "ServiceId": "AevatarSilo",
    "ClientName": "WorkerClient"
  },
  "Redis": {
    "Configuration": "localhost:6379"
  },
  "Dapr": {
    "HttpPort": 3500,
    "GrpcPort": 50001
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## Project Structure

- **Program.cs**: Entry point and host configuration
- **Workers/**: Implementations of specific worker types
- **Handlers/**: Event handlers
- **Services/**: Background service implementations
- **Jobs/**: Job definitions and runners

## Worker Registration

Workers are registered as hosted services in the ABP application:

```csharp
public class AevatarWorkerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // Configure workers
        context.Services.AddHostedService<ScheduledReportWorker>();
        context.Services.AddHostedService<MessageQueueProcessor>();
        context.Services.AddHostedService<DataSyncWorker>();
        
        // Configure event handlers
        Configure<AbpDaprEventBusOptions>(options =>
        {
            options.PubSubName = "pubsub";
        });
    }
}
```

## Scaling

Workers can be scaled horizontally by deploying multiple instances. The Orleans clustering mechanism ensures that distributed operations are coordinated properly, preventing duplicate processing of tasks.

## Monitoring

- Health checks for worker status
- Structured logging with Serilog
- OpenTelemetry integration for metrics and tracing
- Job execution history and status tracking

## Deployment

The worker is designed to be deployed as a containerized service, typically alongside the main Aevatar application but scaled independently based on processing needs.

## Getting Started

1. Ensure MongoDB and other dependencies are running
2. Configure connection strings in `appsettings.json`
3. Run the worker using `dotnet run`
4. Monitor the logs to verify background job execution

## Logging

The worker uses structured logging to provide detailed information about job execution, with special focus on failures, retries, and long-running operations. 