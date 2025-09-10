# Aevatar.Developer.Logger

Advanced logging system for Aevatar development and production environments.

## Overview

This library provides enhanced logging capabilities for the Aevatar platform, with specific features designed to support both development and production scenarios. It integrates with Elasticsearch for log storage and search capabilities.

## Dependencies

- `Elastic.Clients.Elasticsearch` - Client for interacting with Elasticsearch
- `Newtonsoft.Json` - JSON serialization/deserialization library
- `Volo.Abp.Core` - ABP framework core library

## Configuration

Target Framework: .NET 9.0
Namespace: `Aevatar.Developer.Logger`

## Features

- Structured logging with rich metadata
- Elasticsearch integration for log aggregation and search
- Development-specific logging enhancements
- Performance optimized logging pipeline

## Usage

### Basic Setup

```csharp
public class YourModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AevatarLoggerOptions>(options =>
        {
            options.EnableElasticsearch = true;
            options.ElasticsearchUrl = "http://localhost:9200";
            options.IndexPrefix = "aevatar-logs";
        });
    }
}
```

### Logging Example

```csharp
public class YourService
{
    private readonly IAevatarLogger<YourService> _logger;
    
    public YourService(IAevatarLogger<YourService> logger)
    {
        _logger = logger;
    }
    
    public void DoOperation()
    {
        _logger.LogInformation("Operation started");
        
        // Add contextual data to logs
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = Guid.NewGuid(),
            ["UserContext"] = "relevant-data"
        }))
        {
            _logger.LogDebug("Operation details");
            
            // Your operation code
            
            _logger.LogInformation("Operation completed successfully");
        }
    }
}
```

## Best Practices

- Use structured logging with appropriate log levels
- Include relevant context in log events
- Configure appropriate retention policies for Elasticsearch indices
- Consider log volume in production environments 