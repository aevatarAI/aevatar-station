# Orleans & Aevatar Streaming Metrics Dashboard

## Overview

This dashboard provides comprehensive monitoring for Orleans streaming infrastructure and Aevatar custom streaming metrics, covering all streaming-related metrics defined in the [Core Metrics Design Document](../../docs/core-metrics-design.md).

## Dashboard Sections

### 1. Stream Cache Pressure Monitoring
- **Stream Cache Pressure Status**: Binary indicator showing if caches are under pressure
- **Current Cache Pressure**: Gauge showing pressure levels (0-100%)
- **Stream Cache Pressure Over Time**: Trend analysis with threshold indicators

### 2. Queue Cache Statistics
- **Queue Cache Statistics**: Current cache size (bytes) and message counts
- **Cache Message Activity**: Messages added/purged per second
- **Queue Cache Memory Usage**: Memory allocation and release rates

### 3. Pub/Sub Infrastructure
- **Pub/Sub Infrastructure**: Active consumers, producers, and pulling agents
- **Stream Processing Rate**: Messages read/sent per second
- **Stream Message Processing**: Throughput trends over time

### 4. Cache Management & Latency
- **Cache Age & PubSub Cache**: Oldest cache age and pub/sub cache sizes
- **Event Publish Latency Summary**: P50, P95, P99 latency percentiles

## Covered Metrics

### Orleans Streaming Metrics (Custom Implementation)
- `orleans_streams_queue_cache_under_pressure` - Pressure status indicator
- `orleans_streams_queue_cache_pressure` - Current pressure level (0-1)
- `orleans_streams_queue_cache_size_bytes_total` - Cache size in bytes
- `orleans_streams_queue_cache_length_messages_total` - Message count in cache
- `orleans_streams_queue_cache_messages_added_total` - Messages added to cache
- `orleans_streams_queue_cache_messages_purged_total` - Messages purged from cache
- `orleans_streams_queue_cache_memory_allocated_total` - Memory allocated by cache
- `orleans_streams_queue_cache_memory_released_total` - Memory released by cache
- `orleans_streams_queue_cache_oldest_age` - Age of oldest cached message

### Persistent Stream Metrics
- `orleans_streams_persistent_stream_messages_read_total` - Messages read from streams
- `orleans_streams_persistent_stream_messages_sent_total` - Messages sent to streams
- `orleans_streams_persistent_stream_pubsub_cache_size` - Pub/sub cache size
- `orleans_streams_persistent_stream_pulling_agents` - Active pulling agents

### Pub/Sub Infrastructure Metrics
- `orleans_streams_pubsub_consumers_total` - Active consumer count
- `orleans_streams_pubsub_producers_total` - Active producer count

### Aevatar Stream Event Metrics
- `aevatar_stream_event_publish_latency_seconds_*` - Event publish latency histogram

## Template Variables

- **cluster_id**: Filter by Orleans cluster
- **silo_id**: Filter by silo instance
- **queue_id**: Filter by specific queue
- **stream_provider**: Filter by stream provider (e.g., Kafka)

## Thresholds & Alerts

### Pressure Monitoring
- **Green**: Pressure < 70% (Normal operation)
- **Yellow**: Pressure 70-90% (Warning level)
- **Red**: Pressure > 90% (Critical - requires attention)

### Key Performance Indicators
- **Cache Under Pressure**: Should be 0 (False) for healthy operation
- **Message Processing Balance**: Read rate should match sent rate for steady state
- **Memory Usage**: Allocated should generally match released over time
- **Latency**: P95 < 2s, P99 < 5s for good performance

## Usage Instructions

### 1. Upload Dashboard
```bash
./upload-streaming-dashboard.sh
```

### 2. Access Dashboard
- URL: http://localhost:3000/d/aevatar-orleans-streaming-metrics
- Default credentials: admin/admin

### 3. Key Monitoring Points

#### Real-time Health Check
1. Check "Stream Cache Pressure Status" - should show "Normal" (green)
2. Monitor "Current Cache Pressure" gauge - should be < 70%
3. Verify "Pub/Sub Infrastructure" shows expected consumer/producer counts

#### Performance Monitoring
1. Watch "Stream Message Processing" for throughput trends
2. Monitor "Queue Cache Memory Usage" for memory leaks
3. Check "Event Publish Latency Summary" for performance degradation

#### Troubleshooting
1. If pressure > 90%: Check message processing rates and queue backlogs
2. If memory constantly increasing: Investigate memory leaks in cache management
3. If latency spikes: Correlate with pressure metrics and throughput

## Integration with Other Dashboards

This dashboard complements:
- **Orleans Latency Dashboard**: For detailed percentile analysis of publish latency
- **System Metrics**: For correlation with CPU/memory usage
- **Application Logs**: For error context during pressure events

## Prometheus Queries Reference

### Check Stream Pressure
```promql
orleans_streams_queue_cache_pressure{cluster_id="your-cluster"}
```

### Monitor Message Throughput
```promql
rate(orleans_streams_persistent_stream_messages_sent_total[5m])
```

### Calculate Event Publish P95 Latency
```promql
histogram_quantile(0.95, sum(aevatar_stream_event_publish_latency_seconds_bucket) by (le))
```

## Troubleshooting

### No Data Showing
1. Verify streaming services are running and processing messages
2. Check that Kafka provider is active and connected
3. Confirm Prometheus is scraping metrics from otel-collector

### Performance Issues
1. High pressure (>90%): Scale streaming consumers or increase cache limits
2. Memory growth: Check for message leaks or inefficient purging
3. High latency: Investigate network issues or Kafka broker performance

## References

- [Core Metrics Design Document](../../docs/core-metrics-design.md)
- [Orleans Streaming Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/streaming)
- [Kafka Stream Provider Setup](../../docs/kafka-streaming-setup.md)
- [Grafana Dashboard Development](GRAFANA_DASHBOARD_README.md) 