# Aevatar.HttpApi.Host

This project serves as the host application for the Aevatar HTTP APIs, providing the entry point for HTTP requests to the system.

## Overview

`Aevatar.HttpApi.Host` is the host application that runs as a web server exposing the HTTP APIs defined in the `Aevatar.HttpApi` project. It handles cross-cutting concerns such as authentication, logging, exception handling, and serves as the integration point for various infrastructure components.

## Key Features

- API hosting with ASP.NET Core
- Authentication and authorization (JWT-based)
- OpenAPI/Swagger documentation
- Distributed caching (Redis)
- Structured logging with Serilog
- Performance monitoring with OpenTelemetry
- WebSocket/SignalR support
- Response wrapping
- Auto-registration of APIs

## Dependencies

The project leverages the following key dependencies:

- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `Serilog` - Structured logging
- `Volo.Abp.Autofac` - Dependency injection
- `Volo.Abp.Caching.StackExchangeRedis` - Distributed caching
- `Volo.Abp.Swashbuckle` - API documentation
- `Volo.Abp.Account.Web.OpenIddict` - OpenID Connect server
- `Microsoft.Orleans.Client` - Orleans client for grain communication
- `MongoDB.Driver` - MongoDB client
- `AElf.OpenTelemetry` - Observability infrastructure
- `AutoResponseWrapper` - Consistent API response formatting
- `Orleans.Streams.Kafka` - Event streaming

## Related Projects

- `Aevatar.Application` - Application services
- `Aevatar.HttpApi.Admin` - Admin APIs
- `Aevatar.MongoDB` - Data persistence

## Configuration

The application is configured through `appsettings.json` files, which contain settings for:

- Database connections
- Authentication
- CORS policies
- Logging
- Caching
- OpenTelemetry
- Orleans client
- Distributed application settings

## Logging

The application uses Serilog for structured logging, with output to:
- Console
- OpenTelemetry (for centralized observability)

## Deployment

The application is designed to be deployed in containers, and it integrates with:
- Kubernetes for orchestration
- Redis for distributed caching
- MongoDB for persistence
- Kafka for event streaming
- OpenTelemetry for monitoring

## Getting Started

1. Ensure MongoDB and Redis are running
2. Configure connection strings in `appsettings.json`
3. Run the application using `dotnet run`
4. Access the Swagger UI at `/swagger`

## Security

- JWT authentication
- OpenID Connect/OAuth 2.0 via OpenIddict
- HTTPS enforcement
- Proper CORS configuration
- Role-based authorization 