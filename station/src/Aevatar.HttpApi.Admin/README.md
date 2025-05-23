# Aevatar.HttpApi.Admin

HTTP API module for Aevatar's administrative functions.

## Overview

This project exposes HTTP API endpoints for administrative operations within the Aevatar platform. It integrates with the Aevatar application layer and extends the base HTTP API functionality with admin-specific features.

## Dependencies

- `Aevatar.Application.Contracts` - Application contract definitions
- `Aevatar.Application` - Application service implementations
- `Aevatar.HttpApi` - Base HTTP API functionality

## ABP Framework Modules

- `Volo.Abp.Account.HttpApi` - Account management API
- `Volo.Abp.Identity.HttpApi` - Identity management API
- `Volo.Abp.PermissionManagement.HttpApi` - Permission management API

## Configuration

Target Framework: .NET 9.0
Namespace: `Aevatar.Admin`

## Usage

This library is typically consumed by an HTTP API host project that exposes administrative endpoints. It contains controllers that implement the administrative interface of the application. 