# Aevatar.AuthServer Technical Documentation

## Overview
The Aevatar.AuthServer is a .NET authentication and authorization server built on the ABP Framework and OpenIddict. It provides identity management, authentication, and authorization services for the Aevatar platform, supporting various authentication methods including signature-based authentication for blockchain wallets, Google, and Apple sign-in.

## Project Structure

### Project Configuration
- **Target Framework**: .NET 9.0
- **Root Namespace**: Aevatar
- **Project Type**: Web Application (ASP.NET Core)

### Key Components

#### Authentication & Authorization
- **OpenIddict Integration**: Provides OAuth 2.0/OpenID Connect functionality
- **Custom Grant Types**:
  - `signature`: Blockchain wallet-based authentication using cryptographic signatures
  - `google`: Google OAuth authentication
  - `apple`: Apple ID authentication
- **Identity Management**: User management, role-based access control

#### Module Structure
The project is organized as an ABP module (`AevatarAuthServerModule`) with the following dependencies:
- ABP Autofac for dependency injection
- ABP Account modules (Web, Application, HTTP API)
- ABP Identity and Permission Management
- OpenIddict modules for OAuth/OIDC functionality
- MongoDB integration

### Grant Handlers

#### SignatureGrantHandler
Handles authentication via blockchain wallet signatures:
- Validates cryptographic signatures
- Verifies wallet addresses
- Creates or retrieves identity users
- Assigns appropriate roles and claims

#### GoogleGrantHandler
Implements OAuth authentication with Google:
- Verifies Google ID tokens
- Maps Google identities to internal users

#### AppleGrantHandler
Implements Sign in with Apple:
- Verifies Apple ID tokens
- Processes Apple identity claims

### Configuration & Customization
- **Localization**: Supports multiple languages (20+ languages configured)
- **UI Theming**: Uses LeptonXLite theme
- **Auditing**: Configurable auditing options (disabled by default)
- **Data Protection**: Includes data protection configuration
- **Health Checks**: Integrated health monitoring

## Dependencies

### ABP Framework Packages
- Volo.Abp.Account.*
- Volo.Abp.AspNetCore.*
- Volo.Abp.Identity.*
- Volo.Abp.OpenIddict.*
- Volo.Abp.Authorization
- Volo.Abp.Autofac

### External Dependencies
- AElf.Types: For blockchain integration
- Google.Apis.Auth: For Google authentication
- GraphQL.Client: For GraphQL API integration
- Serilog: For structured logging
- StackExchange.Redis: For distributed caching/sessions

### Project References
- Aevatar.Application.Contracts
- Aevatar.Domain.Shared
- Aevatar.MongoDB

## Application Startup

The application uses a standard ASP.NET Core startup pattern with ABP module initialization:
1. Configures Serilog for logging
2. Creates and configures the web host
3. Initializes the ABP application
4. Configures middleware for request processing
5. Starts the application

## Authentication Flow

1. Client requests an access token with credentials specific to the grant type
2. The appropriate grant handler validates the credentials
3. If valid, the handler creates or retrieves the user account
4. Claims are generated and populated (including roles)
5. Access token is issued to the client

## Security Considerations

- Transport security requirements can be disabled for development
- Token expiration is configurable via application settings
- Data protection is configured for secure data storage
- OAuth scopes and resources are properly validated 