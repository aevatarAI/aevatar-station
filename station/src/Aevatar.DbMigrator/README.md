# Aevatar.DbMigrator

Database migration utility for the Aevatar platform.

## Overview

This console application handles database migrations for the Aevatar platform. It applies database schema changes and seeds initial data using ABP's migration system and the MongoDB provider.

## Dependencies

### Project References
- `Aevatar.Application.Contracts` - Application contract definitions
- `Aevatar.MongoDB` - MongoDB database integration

### Packages
- `Serilog.*` - Logging framework
- `Microsoft.Extensions.Hosting` - .NET hosting infrastructure
- `Microsoft.Extensions.Logging` - .NET logging infrastructure
- `Volo.Abp.Autofac` - ABP Autofac integration
- `Volo.Abp.Caching.StackExchangeRedis` - Redis caching integration

## Configuration

Target Framework: .NET 9.0
Namespace: `Aevatar.DbMigrator`
Output Type: Console Application

## Features

- Automated database migration and schema creation
- Initial data seeding (users, roles, permissions)
- Structured logging of migration operations
- Support for different deployment environments through configuration

## Usage

### Running Migrations

```bash
# Run migrations using default configuration
dotnet run

# Run with specific environment
dotnet run --environment Production
```

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "ConnectionStrings": {
    "Default": "mongodb://localhost:27017/Aevatar"
  },
  "Redis": {
    "Configuration": "127.0.0.1"
  },
  "OpenIddict": {
    "Applications": {
      "Aevatar_Web": {
        "ClientId": "web-client",
        "ClientSecret": "client-secret",
        "RootUrl": "https://localhost:44302"
      }
    }
  }
}
```

You can create environment-specific settings files like `appsettings.Production.json` for different environments.

## Development Workflow

1. Make changes to entities or data structures in the domain layer
2. Update the corresponding MongoDB mappings
3. Create or update data seeders as needed
4. Run the DbMigrator to apply changes to the database

## Troubleshooting

### Common Issues

- **Connection failures**: Verify database connection strings and ensure MongoDB is running
- **Seeding errors**: Check seed data for consistency and ensure required data exists
- **Migration errors**: Look for detailed error logs in the Logs directory
- **Permission issues**: Ensure the application has appropriate permissions to MongoDB 