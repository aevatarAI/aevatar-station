# Aevatar.HttpApi

This project contains the HTTP API controllers and endpoints for the Aevatar application.

## Overview

`Aevatar.HttpApi` implements REST API controllers that expose the application's functionality to clients via HTTP. It handles incoming HTTP requests, performs authorization, and delegates to the application layer for business logic processing.

## Key Features

- RESTful API controllers
- Request validation
- Response formatting
- API versioning
- Authorization enforcement
- Endpoint routing

## Dependencies

The project references the following key projects and packages:

- `Aevatar.Application.Contracts` - Application service contracts
- `Aevatar.Application` - Application service implementations
- `Aevatar.Developer.Logger` - Developer-focused logging capabilities
- ABP Framework HTTP API modules:
  - `Volo.Abp.Account.HttpApi`
  - `Volo.Abp.Identity.HttpApi`
  - `Volo.Abp.PermissionManagement.HttpApi`
- `Aevatar.PermissionManagement` - Custom permission management extensions
- `Volo.Abp.AspNetCore.SignalR` - SignalR real-time communication integration

## Project Structure

- **Controllers/**: API controllers organized by domain
- **Models/**: Request and response models specific to the HTTP layer
- **Extensions/**: HTTP-specific extension methods

## API Documentation

API endpoints are documented using Swagger/OpenAPI which is configured in the host project. Controllers use standard ABP Framework conventions for RESTful API design.

## Security

APIs implement proper authorization checks through ABP Framework's permission system. All endpoints enforce appropriate permissions using the `[Authorize]` attribute and permission requirements.

## Logging

API controllers log important requests, responses, and errors at appropriate checkpoints to facilitate debugging and monitoring.

## Usage

This project should be referenced by the HTTP host application that needs to expose the APIs over HTTP. 