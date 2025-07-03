# Aevatar.Aspire

A .NET Aspire orchestration project for the Aevatar platform, providing a streamlined development experience with automated infrastructure and service management.

## Overview

Aevatar.Aspire is the orchestration layer for the Aevatar platform, built on .NET Aspire. It coordinates the startup and communication between various microservices and infrastructure components, providing a seamless development experience.

## Architecture

The Aevatar platform consists of the following components:

### Services

- **AuthServer** - Authentication and authorization service (port 7001)
- **HttpApi.Host** - Main API service (port 7002)
- **Developer.Host** - Developer-focused API and tools (port 7003)
- **Silo** - Orleans-based distributed processing service (ports 11111, 30000)
- **Worker** - Background processing service

### Infrastructure

- **MongoDB** - Document database for persistence
- **Redis** - In-memory data store and caching
- **Elasticsearch** - Search and analytics engine
- **Kafka** - Event streaming platform
- **Qdrant** - Vector database for AI embeddings

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/products/docker-desktop/) for infrastructure containers
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/install)

## Setup

1. Install the .NET Aspire workload:

```bash
dotnet workload install aspire
```

2. Clone the repository:

```bash
git clone https://github.com/yourusername/aevatar-station.git
cd aevatar-station
```

3. Build the solution:

```bash
dotnet build
```

## Usage

### Running the Application

From the `src/Aevatar.Aspire` directory:

```bash
dotnet run
```

This will:
1. Start all infrastructure components (MongoDB, Redis, Elasticsearch, Kafka, Qdrant)
2. Launch all services (AuthServer, HttpApi, Developer.Host, Silo, Worker)
3. Open Swagger UI for the HttpApi and Developer.Host services

### DBMigrator

If is the first time to start docker infrastructure components, you need
run `src/Aevatar.DBMigrator` use docker mongodb connections


### Command-line Options

The application supports the following command-line options:

- `--skip-infrastructure` or `-si`: Skip starting infrastructure resources (useful when they're already running)
- `--help` or `-h`: Display help information

Example:

```bash
# Run without restarting infrastructure
dotnet run --skip-infrastructure

# Show help
dotnet run --help
```

### Accessing Services

Once running, you can access the services at:

- **Aspire Dashboard**: https://localhost:18888
- **HttpApi Swagger**: http://localhost:7002/swagger
- **Developer.Host Swagger**: http://localhost:7003/swagger
- **AuthServer**: http://localhost:7001
- **Orleans Dashboard**: http://localhost:8080 (username: admin, password: 123456)

## Development Workflow

### Making Changes

1. Modify the code in the respective project
2. Rebuild and run the Aspire project
3. Use the `--skip-infrastructure` flag to speed up iterations

### Adding New Services

To add a new service to the orchestration:

1. Create your service project
2. Add a reference to it in the Aevatar.Aspire project
3. Add the service to the `Program.cs` file following the existing pattern

## Configuration

### Orleans Configuration

Orleans is configured with MongoDB for:
- Clustering
- Grain storage
- Event sourcing

The configuration uses the `AevatarOrleans` prefix to avoid conflicts with Orleans' automatic configuration.

### Environment Variables

Key environment variables:
- `ASPNETCORE_ENVIRONMENT`: Set to "Development" by default
- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL`: OpenTelemetry endpoint for the dashboard
- `ASPIRE_ALLOW_UNSECURED_TRANSPORT`: Allows HTTP endpoints

## Troubleshooting

### Common Issues

1. **Infrastructure startup failures**
   - Ensure Docker is running
   - Check if ports are already in use
   - Try running with `--skip-infrastructure` if services are already running

2. **Orleans connectivity issues**
   - Ensure MongoDB is running and accessible
   - Check Orleans configuration in the Silo project

3. **Swagger UI not opening**
   - Manually navigate to http://localhost:7002/swagger or http://localhost:7003/swagger
   - Check if the services are running in the Aspire dashboard

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

[MIT License](LICENSE) 