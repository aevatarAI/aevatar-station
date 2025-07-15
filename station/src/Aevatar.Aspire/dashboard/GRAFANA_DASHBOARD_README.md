# Aevatar Stream Latency Percentiles Dashboard

This document explains how to use the Grafana dashboard for monitoring **Aevatar Stream Event Publish Latency** with percentile metrics (P50, P90, P95, P99).

## 🎯 Overview

The dashboard provides comprehensive latency analysis for high-concurrency stream event publishing in the Aevatar platform using the metric:
- **`aevatar_stream_event_publish_latency_seconds_bucket`**

### Key Features

1. **📊 Percentile Visualization**: P50, P90, P95, P99 percentiles over time
2. **📈 Current Statistics**: Real-time percentile values
3. **🔢 Event Rate Monitoring**: Events published per second
4. **🔥 Latency Heatmap**: Distribution visualization
5. **🎛️ Dynamic Filtering**: By cluster, silo, stream, and namespace

## 🚀 Quick Start

### Prerequisites

1. **Docker Infrastructure Running**:
   ```bash
   cd src/Aevatar.Aspire
   docker compose up -d
   ```

2. **Grafana Access**:
   - URL: http://localhost:3000
   - Username: `admin`
   - Password: `admin`

### Upload Dashboard

#### Option 1: Using Bash Script (Linux/macOS)
```bash
cd src/Aevatar.Aspire
./upload-dashboard.sh
```

#### Option 2: Using PowerShell Script (Windows)
```powershell
cd src/Aevatar.Aspire
.\upload-dashboard.ps1
```

#### Option 3: Manual Upload
1. Open Grafana: http://localhost:3000
2. Login with admin/admin
3. Go to **Dashboards** → **New** → **Import**
4. Copy content from `orleans-latency-dashboard.json`
5. Paste and click **Load**

## 📊 Dashboard Components

### 1. Percentile Time Series
- **Purpose**: Monitor latency percentiles over time
- **Colors**: 
  - 🟢 P50 (Green) - Median latency
  - 🟡 P90 (Yellow) - 90th percentile
  - 🟠 P95 (Orange) - 95th percentile
  - 🔴 P99 (Red) - 99th percentile
- **Usage**: Identify latency spikes and trends

### 2. Current Statistics Panel
- **Purpose**: Real-time percentile values
- **Shows**: Latest P50, P90, P95, P99 values
- **Usage**: Quick health check

### 3. Event Publish Rate
- **Purpose**: Monitor throughput
- **Shows**: Events published per second
- **Usage**: Correlate latency with load

### 4. Latency Distribution Heatmap
- **Purpose**: Visualize latency distribution
- **Shows**: Frequency of different latency values
- **Usage**: Identify latency patterns

## 🎛️ Template Variables

Use these filters to focus on specific components:

| Variable | Description | Example Values |
|----------|-------------|----------------|
| `cluster_id` | Orleans cluster identifier | `AevatarSiloCluster` |
| `silo_id` | Individual silo identifier | `Scheduler_127.0.0.2`, `User_127.0.0.4` |
| `stream_id` | Stream identifier | `UserEvents`, `SystemEvents` |
| `stream_namespace` | Stream namespace | `Aevatar.Events` |

## 🔍 Understanding Metrics

### Percentiles Explained

| Percentile | Meaning | Use Case |
|------------|---------|----------|
| **P50** | 50% of requests are faster | Typical user experience |
| **P90** | 90% of requests are faster | Most users' experience |
| **P95** | 95% of requests are faster | SLA monitoring |
| **P99** | 99% of requests are faster | Outlier detection |

### High Concurrency Considerations

For high-concurrency scenarios, focus on:

1. **P95/P99 Stability**: These indicate system behavior under load
2. **Rate vs Latency**: Monitor if increased throughput affects latency
3. **Heatmap Patterns**: Look for bimodal distributions (dual peaks)

## 📈 Prometheus Queries

The dashboard uses these key queries:

### Percentile Query Pattern
```promql
histogram_quantile(0.95, 
  sum(rate(aevatar_stream_event_publish_latency_seconds_bucket[5m])) 
  by (le, cluster_id, silo_id, stream_id, stream_namespace)
)
```

### Event Rate Query
```promql
sum(rate(aevatar_stream_event_publish_latency_seconds_count[5m]))
```

## 🚨 Alerting Recommendations

Consider setting up alerts for:

1. **High P95 Latency**: `> 100ms` for 5 minutes
2. **High P99 Latency**: `> 500ms` for 2 minutes
3. **Zero Event Rate**: `= 0` for 1 minute (service down)
4. **Latency Spike**: P95 > 3x baseline for 2 minutes

## 🔧 Troubleshooting

### Common Issues

#### 1. No Data Displayed
- ✅ Check if Prometheus is scraping metrics
- ✅ Verify `aevatar_stream_event_publish_latency_seconds_bucket` exists
- ✅ Ensure time range includes data

#### 2. Dashboard Upload Fails
- ✅ Check Grafana is running: `docker ps | grep grafana`
- ✅ Verify credentials (admin/admin)
- ✅ Check JSON syntax in dashboard file

#### 3. Authentication Errors
- ✅ Use default credentials: admin/admin
- ✅ Check if Grafana has custom auth configured

### Testing Metrics

To verify metrics are being collected:

1. **Check Prometheus Targets**:
   ```bash
   curl http://localhost:9091/api/v1/targets
   ```

2. **Query Metric Directly**:
   ```bash
   curl "http://localhost:9091/api/v1/query?query=aevatar_stream_event_publish_latency_seconds_bucket"
   ```

## 🎯 Best Practices

### For High-Concurrency Monitoring

1. **Use Appropriate Time Windows**:
   - Real-time: 5-15 minute windows
   - Analysis: 1-24 hour windows

2. **Monitor Multiple Percentiles**:
   - P50: Typical performance
   - P95: SLA compliance
   - P99: Outlier detection

3. **Correlate with Business Metrics**:
   - User activity patterns
   - System resource usage
   - Error rates

### Dashboard Usage Tips

1. **Zoom In on Incidents**: Use time picker to focus on specific periods
2. **Filter by Component**: Use template variables to isolate issues
3. **Compare Across Silos**: Multi-select silos to compare performance
4. **Export Data**: Use panel menu to export data for analysis

## 📚 Additional Resources

- [Prometheus Histogram Queries](https://prometheus.io/docs/practices/histograms/)
- [Grafana Dashboard Best Practices](https://grafana.com/docs/grafana/latest/dashboards/manage-dashboards/)
- [Orleans Metrics Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/host/monitoring/)

## 🔄 Updating the Dashboard

To update the dashboard:

1. Modify `orleans-latency-dashboard.json`
2. Run upload script again
3. Existing dashboard will be automatically updated

## 🆘 Support

If you encounter issues:

1. Check Grafana logs: `docker logs grafana`
2. Verify Prometheus metrics: http://localhost:9091/targets
3. Test connectivity: `curl http://localhost:3000/api/health`

---

**Dashboard Version**: 1.0  
**Last Updated**: $(date)  
**Compatible with**: Grafana 8.0+, Prometheus 2.0+ 