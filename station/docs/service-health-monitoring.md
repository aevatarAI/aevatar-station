# Service Health Monitoring

## Overview

The Aevatar Station platform now provides comprehensive service health monitoring by leveraging existing `/health` endpoints and Prometheus scraping. This implementation provides `service_up` metrics and derived availability/downtime calculations without requiring custom application code.

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   HTTP Services │    │   Prometheus    │    │     Grafana     │
│                 │    │                 │    │                 │
│ • HttpApi:7002  │───▶│ Scrapes /health │───▶│ Visualizes      │
│ • AuthServer:7001│    │ every 30s       │    │ service_up      │
│ • Developer:7003 │    │                 │    │ metrics         │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Monitored Services

| Service | Port | Health Endpoint | Service Label |
|---------|------|-----------------|---------------|
| HttpApi.Host | 7002 | `/health` | `aevatar-httpapi` |
| AuthServer | 7001 | `/health` | `aevatar-authserver` |
| Developer.Host | 7003 | `/health` | `aevatar-developer` |

## Available Metrics

### Base Metrics

- **`service_up`**: Service availability status (1 = up, 0 = down)
  - Labels: `service`, `service_type`, `instance`
  - Scraped every 30 seconds from `/health` endpoints

### Derived Metrics (Prometheus Recording Rules)

- **`service_availability_percentage_1h`**: Service availability percentage over 1 hour
- **`service_availability_percentage_24h`**: Service availability percentage over 24 hours  
- **`service_availability_percentage_7d`**: Service availability percentage over 7 days
- **`service_downtime_seconds_total_1h`**: Total downtime in seconds over 1 hour
- **`service_downtime_seconds_total_24h`**: Total downtime in seconds over 24 hours
- **`service_uptime_seconds_total_1h`**: Total uptime in seconds over 1 hour
- **`service_uptime_seconds_total_24h`**: Total uptime in seconds over 24 hours

### Alert Metrics

- **`service_down_duration_seconds`**: How long a service has been down
- **`service_flapping_rate_1h`**: Rate of service up/down changes (flapping detection)
- **`service_recovery_time_seconds`**: Time taken for service to recover after being down

## Configuration Files

### Prometheus Configuration
- **File**: `src/Aevatar.Aspire/prometheus.yml`
- **Health Check Jobs**: Separate scrape jobs for each service's `/health` endpoint
- **Relabeling**: Transforms Prometheus `up` metric to `service_up` with proper labels

### Recording Rules
- **File**: `src/Aevatar.Aspire/prometheus-rules.yml`
- **Evaluation**: Rules evaluated every 30 seconds
- **Time Windows**: 1h, 24h, and 7d availability calculations

## Usage Examples

### Prometheus Queries

```promql
# Check current service status
service_up

# Get availability percentage for last 24 hours
service_availability_percentage_24h

# Get total downtime for last hour
service_downtime_seconds_total_1h

# Check for services that are currently down
service_up == 0

# Get services with availability < 99% in last hour
service_availability_percentage_1h < 99
```

### Grafana Dashboard Queries

```promql
# Service uptime gauge
service_up

# Availability percentage over time
service_availability_percentage_1h

# Downtime incidents
increase(service_downtime_seconds_total_1h[1h])

# Service flapping detection
service_flapping_rate_1h > 0.1
```

## Alerting Rules

You can create alerts based on these metrics:

```yaml
groups:
  - name: service_health_alerts
    rules:
      - alert: ServiceDown
        expr: service_up == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Service {{ $labels.service }} is down"
          description: "Service {{ $labels.service }} has been down for more than 1 minute"

      - alert: LowAvailability
        expr: service_availability_percentage_1h < 95
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Service {{ $labels.service }} has low availability"
          description: "Service {{ $labels.service }} availability is {{ $value }}% in the last hour"
```

## Deployment

1. **Update Prometheus**: The configuration is automatically loaded when Aspire starts
2. **Restart Services**: No application changes needed - uses existing `/health` endpoints
3. **Verify Metrics**: Check Prometheus targets page to ensure health endpoints are being scraped

## Validation

```bash
# Check if health endpoints are accessible
curl http://localhost:7001/health  # AuthServer
curl http://localhost:7002/health  # HttpApi.Host  
curl http://localhost:7003/health  # Developer.Host

# Check Prometheus targets
curl http://localhost:9091/targets

# Query service_up metrics
curl -s "http://localhost:9091/api/v1/query?query=service_up" | jq
```

## Benefits

1. **Zero Application Changes**: Uses existing `/health` endpoints
2. **Standard Prometheus Pattern**: Follows industry best practices
3. **Rich Metrics**: Provides availability, downtime, and alerting metrics
4. **Scalable**: Easy to add new services by updating Prometheus config
5. **Observable**: Full visibility into service health across the platform 