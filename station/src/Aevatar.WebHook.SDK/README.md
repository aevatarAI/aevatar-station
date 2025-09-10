# Aevatar.WebHook.SDK

This project provides a Software Development Kit (SDK) for integrating with Aevatar's webhook system.

## Overview

`Aevatar.WebHook.SDK` offers a comprehensive set of tools to easily implement and consume Aevatar webhooks. It provides the foundation for creating webhook integrations, allowing external systems to receive event notifications from the Aevatar platform and respond to those events programmatically.

## Key Features

- Webhook registration and management
- Event payload serialization and deserialization
- Authentication and verification
- Webhook event subscription
- Test and debug utilities
- Retry and error handling
- Event validation

## Dependencies

The project leverages the following key dependencies:

- `Serilog.AspNetCore` - Structured logging for ASP.NET Core
- `Volo.Abp.AspNetCore.Serilog` - ABP Framework Serilog integration
- `Volo.Abp.Autofac` - Dependency injection
- `Volo.Abp.AspNetCore.Mvc` - MVC components
- `Volo.Abp.AutoMapper` - Object mapping capabilities
- `Aevatar.Core` - Core Aevatar platform utilities
- `Aevatar.Core.Abstractions` - Core abstractions and interfaces

## Project Structure

- **Models/**: Webhook payload and configuration models
- **Handlers/**: Webhook event handlers
- **Extensions/**: Extension methods for webhook integration
- **Security/**: Authentication and verification utilities
- **Services/**: Webhook service implementations

## Event Types

The SDK supports various webhook event types:

- System events (user creation, updates, etc.)
- Business process events
- Integration events
- Custom user-defined events

## Usage

### Registering a Webhook

```csharp
public class MyWebhookModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<WebhookOptions>(options =>
        {
            options.Webhooks.Add(new WebhookDefinition(
                name: "UserCreatedWebhook",
                displayName: "User Created Webhook",
                description: "Triggered when a new user is created")
            );
        });
    }
}
```

### Subscribing to Webhook Events

```csharp
public class WebhookSubscriber : ITransientDependency
{
    private readonly IWebhookSubscriptionManager _subscriptionManager;
    
    public WebhookSubscriber(IWebhookSubscriptionManager subscriptionManager)
    {
        _subscriptionManager = subscriptionManager;
    }
    
    public async Task SubscribeToUserEventsAsync(string tenantId, string webhookUri)
    {
        await _subscriptionManager.SubscribeAsync(
            tenantId: tenantId,
            webhookName: "UserCreatedWebhook",
            webhookUri: webhookUri,
            headers: new Dictionary<string, string>
            {
                {"Authorization", "Bearer YOUR_SECRET_TOKEN"}
            }
        );
    }
}
```

### Triggering Webhook Events

```csharp
public class UserService
{
    private readonly IWebhookPublisher _webhookPublisher;
    
    public UserService(IWebhookPublisher webhookPublisher)
    {
        _webhookPublisher = webhookPublisher;
    }
    
    public async Task CreateUserAsync(UserDto user)
    {
        // Create user in the system...
        
        await _webhookPublisher.PublishAsync(
            webhookName: "UserCreatedWebhook",
            data: user,
            tenantId: CurrentTenant.Id
        );
    }
}
```

## Security

The SDK implements security best practices:
- Webhook payloads can be signed for authenticity verification
- Secure webhook secret management
- TLS for all webhook communications
- Rate limiting capabilities

## Logging

Webhook operations log important events and errors at appropriate checkpoints to facilitate debugging and monitoring. 