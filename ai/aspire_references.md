# Aspire Integration Guide

## Service Endpoints

| Service | Endpoint | Port | Description |
|---------|----------|------|-------------|
| **Aspire Dashboard** | `https://localhost:18888` | 18888 | .NET Aspire Dashboard for monitoring services |
| **AuthServer** | `http://localhost:7001` | 7001 | Authentication service |
| **HttpApi.Host** | `http://localhost:7002` | 7002 | Main API with Swagger UI |
| **Developer.Host** | `http://localhost:7003` | 7003 | Developer API with Swagger UI |
| **MongoDB** | `mongodb://localhost:27017` | 27017 | Document database |
| **Redis** | `localhost:6379` | 6379 | Caching and data store |
| **Kafka** | `localhost:9092` | 9092 | Event streaming |
| **Elasticsearch** | `http://localhost:9200` | 9200 | Search engine |
| **Qdrant** | `http://localhost:6333` | 6333 | Vector database |
| **Jaeger** | `http://localhost:16686` | 16686 | Distributed tracing UI |
| **Prometheus** | `http://localhost:9091` | 9091 | Metrics monitoring |
| **Grafana** | `http://localhost:3000` | 3000 | Metrics visualization and dashboards |

## Authentication Flow

1. **Start Aspire Dashboard**: `dotnet run` in the Aspire project directory
2. **Dashboard Login**: When console displays `Login to the dashboard at https://localhost:18888/login?t=<token>`, open the URL
3. **Retrieve Auth Token**: Once dashboard is accessible, get authentication key from AuthServer API: `http://localhost:7001/connect/token`
4. **Use Auth Token**: Include this token in all authenticated API requests via Bearer authentication

## Orleans Silos

Three Orleans silos run on different loopback IP addresses:
- Scheduler: 127.0.0.4:11111 (Gateway: 30000, Dashboard: 8080)
- Projector: 127.0.0.2:11112 (Gateway: 30001, Dashboard: 8081)
- User: 127.0.0.3:11113 (Gateway: 30002, Dashboard: 8082)

> **Note**: Configure loopback IP aliases with: `sudo ifconfig lo0 alias 127.0.0.2` and similar commands.

## Infrastructure Components

All services are configured with Docker:
- **MongoDB**: Document database
- **Redis**: Caching and data store
- **Elasticsearch**: Search engine  
- **Kafka**: Event streaming
- **Qdrant**: Vector database for AI embeddings
- **Jaeger**: Distributed tracing system for monitoring and troubleshooting microservices
- **Prometheus**: Time series database for metrics collection and alerting
- **Grafana**: Analytics and monitoring platform for visualizing metrics
