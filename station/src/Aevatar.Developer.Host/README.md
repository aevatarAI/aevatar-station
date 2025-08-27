# Aevatar.Developer.Host

## Overview

The Aevatar.Developer.Host project serves as the main web hosting application for the Aevatar Developer platform. It provides API endpoints, authentication, and integration with various services including Orleans distributed computing framework, MongoDB, SignalR for real-time communication, and OpenTelemetry for observability.

## Technology Stack

- **.NET 9.0**: Latest .NET runtime
- **ASP.NET Core**: Web framework for building APIs
- **ABP Framework**: Application framework providing modular architecture
- **Orleans**: Distributed computing framework for building scalable applications
- **MongoDB**: NoSQL database for data persistence
- **OpenTelemetry**: Observability framework for monitoring and tracing
- **Serilog**: Structured logging provider
- **SignalR**: Real-time communications library
- **Swagger/OpenAPI**: API documentation

## Project Structure

- **Program.cs**: Application entry point that configures logging, Orleans client, and web host
- **AevatarDeveloperHostModule.cs**: Main ABP module that configures services including authentication, CORS, Swagger, etc.
- **Extensions/**: Contains extension methods for the application
- **appsettings.json**: Configuration settings for the application

## Key Features

- **API Endpoints**: RESTful API endpoints for developer interactions
- **Authentication/Authorization**: JWT-based authentication
- **Real-time Communication**: SignalR hub for real-time messaging
- **API Documentation**: Swagger UI for exploring and testing APIs
- **Distributed Computing**: Orleans client for connecting to Orleans clusters
- **Health Checks**: Endpoint for monitoring application health
- **Structured Logging**: Serilog integration for comprehensive logging
- **Observability**: OpenTelemetry integration for distributed tracing

## Configuration

The application can be configured via `appsettings.json` with settings for:

- MongoDB connection strings
- Orleans cluster settings
- Authentication server settings
- OpenTelemetry configuration
- Logging settings
- HTTP endpoint configuration

## Dependencies

The application depends on several other projects:
- Aevatar.Application
- Aevatar.HttpApi
- Aevatar.MongoDB

## Running the Application

The application listens on port 8308 by default and requires:
- MongoDB instance (default: mongodb://localhost:27017)
- Orleans cluster
- Redis (optional, for distributed caching) 