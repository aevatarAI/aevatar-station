# Aevatar.HttpApi.Client

HTTP API client library for consuming Aevatar services.

## Overview

This project provides client proxies to consume Aevatar HTTP APIs. It automatically generates client-side proxies for API endpoints, making it easy to interact with the Aevatar backend from client applications.

## Dependencies

- `Aevatar.Application.Contracts` - Application contract definitions

## ABP Framework Modules

- `Volo.Abp.Account.HttpApi.Client` - Account management client
- `Volo.Abp.Identity.HttpApi.Client` - Identity management client
- `Volo.Abp.PermissionManagement.HttpApi.Client` - Permission management client

## Configuration

Target Framework: .NET 9.0
Namespace: `Aevatar`

## Features

- Auto-generated API proxies
- Type-safe client for Aevatar backend services
- Integration with ABP dynamic HTTP API client system

## Usage

### Client Application Setup

1. Reference this library in your client application
2. Configure ABP HTTP client in your module:

```csharp
[DependsOn(typeof(AevatarHttpApiClientModule))]
public class YourClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure remote API endpoint
        Configure<AbpRemoteServiceOptions>(options =>
        {
            options.RemoteServices.Default = new RemoteServiceConfiguration("https://api.example.com/");
        });
    }
}
```

3. Inject and use the HTTP client interfaces in your application code.

## Proxy Generation

The project includes embedded proxy generation configuration files that define how client proxies are generated. 