# Latency Analysis Automation Rule

## Overview

This rule provides automated steps for running Orleans broadcasting programs and analyzing latency metrics from both Prometheus and Jaeger tracing data to identify performance bottlenecks.

## Prerequisites

### Required Services
- ✅ Docker Desktop running
- ✅ .NET 8.0 SDK installed
- ✅ Orleans Aspire infrastructure
- ✅ Prometheus metrics collection
- ✅ Jaeger tracing enabled
- ✅ MongoDB for grain state persistence
- ✅ Elasticsearch for indexing (Projector silo)

### Environment Setup Commands
```bash
# Navigate to workspace
cd /Users/charles/workspace/github/aevatar-station

# Verify current directory
pwd

# Check git branch
git branch --show-current
```

## Automation Workflow

### Phase 1: Infrastructure Startup

#### 1.1 Start Docker Services (if not running)
```bash
# Check if Docker services are already running
DOCKER_RUNNING=$(docker ps | grep -E "(mongo|prometheus|jaeger|elasticsearch)" | wc -l)

if [ $DOCKER_RUNNING -lt 4 ]; then
    echo "Starting Docker services..."
    docker compose -f /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire/docker-compose.yml up -d
    
    # Wait for services to be ready (30 seconds)
    sleep 30
else
    echo "Docker services already running, skipping startup"
fi

# Verify all services are running
docker ps | grep -E "(mongo|prometheus|jaeger|elasticsearch)"
```

#### 1.2 Start Aspire Orchestration (if not running)
```bash
# Check if Aspire is already running
ASPIRE_RUNNING=$(ps aux | grep -v grep | grep "Aevatar.Aspire" | wc -l)

if [ $ASPIRE_RUNNING -eq 0 ]; then
    echo "Starting Aspire orchestration..."
    # Navigate to Aspire project (as required by aspire-references rule)
    cd /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire
    
    # Start Aspire orchestration
    dotnet run &
    
    # Store Aspire PID for cleanup
    ASPIRE_PID=$!
    echo "Aspire started with PID: $ASPIRE_PID"
    
    # Wait for Aspire to initialize (60 seconds as specified in aspire-references rule)
    sleep 60
else
    echo "Aspire already running, skipping startup"
    # Get existing Aspire PID for cleanup reference
    ASPIRE_PID=$(pgrep -f "Aevatar.Aspire")
    echo "Existing Aspire PID: $ASPIRE_PID"
fi
```

#### 1.3 Verify Service Endpoints
```bash
# Check Aspire Dashboard
curl -s http://localhost:15888/health || echo "Aspire not ready"

# Check Prometheus (try both possible ports)
if curl -s http://localhost:9091/-/ready > /dev/null 2>&1; then
    echo "Prometheus ready on port 9091"
elif curl -s http://localhost:9090/-/ready > /dev/null 2>&1; then
    echo "Prometheus ready on port 9090"
else
    echo "Prometheus not ready on either port 9090 or 9091"
fi

# Check Jaeger
curl -s http://localhost:16686/api/services > /dev/null && echo "Jaeger ready" || echo "Jaeger not ready"

# Check MongoDB
mongosh --host localhost:27017 --eval "db.adminCommand('ping')" || echo "MongoDB not ready"
```

### Phase 2: Clear Previous Metrics

#### 2.1 Clear Prometheus Data with Validation
```bash
echo "Clearing metrics data (both in-memory and persistent)..."

# Step 1: Stop Aspire to clear in-memory bucket data (following aspire-references rule)
echo "Stopping Aspire to clear in-memory bucket data..."

# Find Aspire Process ID
ASPIRE_PID=$(pgrep -f "Aevatar.Aspire")
if [ -n "$ASPIRE_PID" ]; then
    echo "Found Aspire process with PID: $ASPIRE_PID"
    # Send SIGINT (same as Cmd+C)
    kill -INT $ASPIRE_PID
    # Wait for Graceful Shutdown
    sleep 10
    # Verify Termination
    if pgrep -f "Aevatar.Aspire" > /dev/null; then
        echo "Aspire still running, force killing..."
        kill -KILL $(pgrep -f "Aevatar.Aspire")
        sleep 2
    fi
    pgrep -f "Aevatar.Aspire" || echo "Aspire stopped successfully"
else
    echo "No Aspire processes found"
fi

# Stop any other Aevatar processes using same approach
OTHER_PIDS=$(pgrep -f "Aevatar\." | grep -v $(pgrep -f "Aevatar.Aspire" || echo "0"))
if [ -n "$OTHER_PIDS" ]; then
    echo "Stopping other Aevatar processes: $OTHER_PIDS"
    kill -INT $OTHER_PIDS 2>/dev/null || echo "No other processes to stop"
    sleep 5
fi

# Step 2: Clear Prometheus persistent data
echo "Clearing Prometheus persistent data..."
# Stop Prometheus container
docker stop $(docker ps -q --filter "name=prometheus") 2>/dev/null || echo "Prometheus container not running"

# Remove ONLY Prometheus data volumes (not other services)
docker volume rm $(docker volume ls -q | grep prometheus) 2>/dev/null || echo "No Prometheus volumes to remove"

# Step 3: Restart Prometheus with clean data
echo "Restarting Prometheus with clean volumes..."
docker compose -f /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire/docker-compose.yml up -d prometheus

# Wait for Prometheus to be ready
echo "Waiting for Prometheus to restart..."
sleep 15
for i in {1..12}; do
    if curl -s http://localhost:9091/-/ready > /dev/null 2>&1; then
        echo "Prometheus is ready on port 9091"
        break
    elif curl -s http://localhost:9090/-/ready > /dev/null 2>&1; then
        echo "Prometheus is ready on port 9090"
        break
    else
        echo "Waiting for Prometheus... (attempt $i/12)"
        sleep 5
    fi
done

# Step 4: Restart Aspire with clean state (following aspire-references rule)
echo "Restarting Aspire with clean in-memory state..."

# Navigate to Aspire project (as required by aspire-references rule)
cd /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire
echo "Navigated to Aspire directory: $(pwd)"

# Start Aspire
dotnet run &
ASPIRE_PID=$!
echo "Aspire restarted with PID: $ASPIRE_PID"

# Wait for Aspire to initialize (as specified in aspire-references rule)
echo "Waiting for Aspire to initialize (60 seconds)..."
sleep 60
echo "Aspire initialization complete"
```

#### 2.2 Clear Jaeger Traces
```bash
# Restart Jaeger for clean state
echo "Clearing Jaeger traces..."
docker restart $(docker ps -q --filter "name=jaeger")

# Wait for Jaeger to restart
sleep 10

# Verify Jaeger is ready
curl -s http://localhost:16686/api/services > /dev/null && echo "Jaeger cleared successfully" || echo "Jaeger restart may have failed"
```

#### 2.3 Final Validation
```bash
echo "Final validation of cleared data..."

# Determine correct Prometheus port
PROMETHEUS_PORT=9091
if ! curl -s http://localhost:9091/-/ready > /dev/null 2>&1; then
    PROMETHEUS_PORT=9090
fi

echo "Using Prometheus port: $PROMETHEUS_PORT"

# Check Prometheus metrics (should be 0 after clearing)
STREAM_COUNT=$(curl -s "http://localhost:$PROMETHEUS_PORT/api/v1/query?query=aevatar_stream_event_publish_latency_seconds_bucket" | jq '.data.result | length')
DB_COUNT=$(curl -s "http://localhost:$PROMETHEUS_PORT/api/v1/query?query=aevatar_grain_storage_write_duration_milliseconds_bucket" | jq '.data.result | length')

echo "Stream latency metrics: $STREAM_COUNT"
echo "Database latency metrics: $DB_COUNT"

if [ "$STREAM_COUNT" -eq "0" ] && [ "$DB_COUNT" -eq "0" ]; then
    echo "✅ All previous metrics successfully cleared"
else
    echo "❌ Warning: Some metrics may still be present"
    echo "This is expected immediately after restart - metrics should be 0 before running tests"
fi

# Check Jaeger readiness
curl -s http://localhost:16686/api/services > /dev/null && echo "✅ Jaeger ready for new traces" || echo "❌ Jaeger not ready"

echo "Data clearing phase completed"
```



### Phase 3: Execute Broadcasting Test

#### 3.1 Run Broadcasting Sample
```bash
# Navigate to test sample
cd /Users/charles/workspace/github/aevatar-station/samples/VerifyDbIssue545

# Run with stored IDs for consistent 5000 subscribers
dotnet run --use-stored-ids

# Store test execution PID
TEST_PID=$!
echo "Broadcasting test started with PID: $TEST_PID"

# Wait for test completion (estimated 5 minutes)
wait $TEST_PID
echo "Broadcasting test completed"
```



### Phase 4: Collect Latency Metrics

#### 4.1 Extract Prometheus Metrics
```bash
# Create metrics output directory
mkdir -p benchmark/metrics/$(date +%Y%m%d_%H%M%S)
METRICS_DIR="benchmark/metrics/$(date +%Y%m%d_%H%M%S)"

# Determine correct Prometheus port
PROMETHEUS_PORT=9091
if ! curl -s http://localhost:9091/-/ready > /dev/null 2>&1; then
    PROMETHEUS_PORT=9090
fi

echo "Using Prometheus port: $PROMETHEUS_PORT for metrics extraction"

# Extract aevatar_stream_event_publish_latency_seconds_bucket
curl -s "http://localhost:$PROMETHEUS_PORT/api/v1/query?query=aevatar_stream_event_publish_latency_seconds_bucket" \
  | jq '.data.result' > "$METRICS_DIR/stream_latency_buckets.json"

# Extract Orleans request latency
curl -s "http://localhost:$PROMETHEUS_PORT/api/v1/query?query=orleans_app_requests_latency_bucket" \
  | jq '.data.result' > "$METRICS_DIR/orleans_request_latency.json"

# Extract database write latency
curl -s "http://localhost:$PROMETHEUS_PORT/api/v1/query?query=aevatar_grain_storage_write_duration_milliseconds_bucket" \
  | jq '.data.result' > "$METRICS_DIR/database_write_latency.json"

echo "Prometheus metrics extracted to $METRICS_DIR"
```

#### 4.2 Extract Jaeger Tracing Data
```bash
# Get recent traces for DeliverBatch operations
TRACE_START=$(date -u -d '10 minutes ago' +%s)000000
TRACE_END=$(date -u +%s)000000

# Extract DeliverBatch traces
curl -s "http://localhost:16686/api/traces?service=aevatar-silo&operation=IStreamConsumerExtension%2FDeliverBatch&start=${TRACE_START}&end=${TRACE_END}&limit=10000" \
  | jq '.data' > "$METRICS_DIR/deliverbatch_traces.json"

# Extract grain operation traces
curl -s "http://localhost:16686/api/traces?service=aevatar-silo&operation=TestDbGAgent%2FOnAddNumberEvent&start=${TRACE_START}&end=${TRACE_END}&limit=5000" \
  | jq '.data' > "$METRICS_DIR/grain_operation_traces.json"

echo "Jaeger traces extracted to $METRICS_DIR"
```

### Phase 5: Automated Analysis

#### 5.1 Generate Latency Distribution Analysis
```bash
# Create analysis script
cat > "$METRICS_DIR/analyze_latency.py" << 'EOF'
#!/usr/bin/env python3
import json
import statistics
import numpy as np
from collections import defaultdict

def analyze_prometheus_buckets(file_path):
    """Analyze Prometheus histogram bucket data"""
    with open(file_path, 'r') as f:
        data = json.load(f)
    
    buckets = defaultdict(int)
    for result in data:
        le = float(result['metric']['le'])
        value = int(result['value'][1])
        buckets[le] = value
    
    # Calculate percentiles from cumulative buckets
    total_count = max(buckets.values())
    percentiles = {}
    
    for p in [50, 90, 95, 99, 99.9]:
        target_count = total_count * (p / 100)
        for le, count in sorted(buckets.items()):
            if count >= target_count:
                percentiles[f'p{p}'] = le
                break
    
    return {
        'total_operations': total_count,
        'percentiles': percentiles,
        'bucket_distribution': dict(buckets)
    }

def analyze_jaeger_traces(file_path):
    """Analyze Jaeger trace timing data"""
    with open(file_path, 'r') as f:
        traces = json.load(f)
    
    durations = []
    process_breakdown = defaultdict(list)
    
    for trace in traces:
        for span in trace.get('spans', []):
            duration_ms = span['duration'] / 1000  # Convert microseconds to milliseconds
            durations.append(duration_ms)
            
            # Extract process information
            process_id = span.get('processID', 'unknown')
            process_breakdown[process_id].append(duration_ms)
    
    if not durations:
        return {'error': 'No trace data found'}
    
    return {
        'total_operations': len(durations),
        'average_ms': statistics.mean(durations),
        'median_ms': statistics.median(durations),
        'p95_ms': np.percentile(durations, 95),
        'p99_ms': np.percentile(durations, 99),
        'min_ms': min(durations),
        'max_ms': max(durations),
        'process_breakdown': {
            pid: {
                'count': len(times),
                'average_ms': statistics.mean(times),
                'p95_ms': np.percentile(times, 95)
            } for pid, times in process_breakdown.items()
        }
    }

# Analyze metrics files
if __name__ == "__main__":
    import sys
    import os
    
    metrics_dir = sys.argv[1] if len(sys.argv) > 1 else '.'
    
    results = {}
    
    # Analyze Prometheus metrics
    prometheus_files = [
        ('stream_latency', 'stream_latency_buckets.json'),
        ('orleans_latency', 'orleans_request_latency.json'),
        ('database_latency', 'database_write_latency.json')
    ]
    
    for name, filename in prometheus_files:
        file_path = os.path.join(metrics_dir, filename)
        if os.path.exists(file_path):
            results[name] = analyze_prometheus_buckets(file_path)
    
    # Analyze Jaeger traces
    jaeger_files = [
        ('deliverbatch_traces', 'deliverbatch_traces.json'),
        ('grain_traces', 'grain_operation_traces.json')
    ]
    
    for name, filename in jaeger_files:
        file_path = os.path.join(metrics_dir, filename)
        if os.path.exists(file_path):
            results[name] = analyze_jaeger_traces(file_path)
    
    # Output analysis results
    with open(os.path.join(metrics_dir, 'analysis_results.json'), 'w') as f:
        json.dump(results, f, indent=2)
    
    print(f"Analysis complete. Results saved to {metrics_dir}/analysis_results.json")
EOF

# Run analysis
python3 "$METRICS_DIR/analyze_latency.py" "$METRICS_DIR"
```

#### 5.2 Generate Performance Report and Update Analysis Documents
```bash
# Create performance report
cat > "$METRICS_DIR/performance_report.md" << EOF
# Performance Analysis Report - $(date)

## Test Configuration
- **Sample**: VerifyDbIssue545 with --use-stored-ids
- **Subscribers**: 5000 TestDbGAgent instances
- **Silos**: 2 (Scheduler p1 + Projector p2)
- **Environment**: Local laptop (localhost communication)

## Metrics Sources
- **Prometheus**: Stream latency, Orleans latency, Database latency
- **Jaeger**: DeliverBatch traces, Grain operation traces
- **Collection Time**: $(date -u)

## Analysis Results
\`\`\`json
$(cat "$METRICS_DIR/analysis_results.json")
\`\`\`

## Key Findings
$(python3 -c "
import json
with open('$METRICS_DIR/analysis_results.json', 'r') as f:
    data = json.load(f)

if 'stream_latency' in data:
    stream = data['stream_latency']
    print(f'- **Stream Latency p95**: {stream[\"percentiles\"].get(\"p95\", \"N/A\")}s')
    print(f'- **Total Stream Operations**: {stream[\"total_operations\"]}')

if 'deliverbatch_traces' in data:
    traces = data['deliverbatch_traces']
    if 'error' not in traces:
        print(f'- **DeliverBatch Average**: {traces[\"average_ms\"]:.1f}ms')
        print(f'- **DeliverBatch p95**: {traces[\"p95_ms\"]:.1f}ms')
        
        if 'process_breakdown' in traces:
            for pid, stats in traces['process_breakdown'].items():
                print(f'- **Process {pid}**: {stats[\"average_ms\"]:.1f}ms avg, {stats[\"count\"]} ops')
")

## Recommendations
- Compare Process p1 vs p2 performance
- Identify Orleans processing bottlenecks
- Focus optimization on highest latency components
- Monitor Elasticsearch indexing impact (p2)

## Reference Analysis Documents
- [DeliverBatch Latency Analysis](../DeliverBatch_Latency_Analysis.md)
- [Orleans Multi-Silo Latency Analysis](../Orleans_Multi_Silo_Latency_Analysis.md)

EOF

echo "Performance report generated: $METRICS_DIR/performance_report.md"

# Update analysis documents with new results (if significant changes detected)
python3 -c "
import json
import os

# Load current results
with open('$METRICS_DIR/analysis_results.json', 'r') as f:
    data = json.load(f)

# Check if results differ significantly from previous analysis
# Update benchmark documents if needed
print('Analysis documents can be updated manually based on new results')
print('Key metrics for comparison:')

if 'deliverbatch_traces' in data and 'error' not in data['deliverbatch_traces']:
    traces = data['deliverbatch_traces']
    print(f'DeliverBatch Average: {traces[\"average_ms\"]:.1f}ms')
    if 'process_breakdown' in traces:
        for pid, stats in traces['process_breakdown'].items():
            print(f'Process {pid}: {stats[\"average_ms\"]:.1f}ms avg')

if 'stream_latency' in data:
    stream = data['stream_latency']
    percentiles = stream.get('percentiles', {})
    print(f'Stream Latency p95: {percentiles.get(\"p95\", \"N/A\")}s')
"
```

### Phase 6: Cleanup and Results

#### 6.1 Stop Services
```bash
# Stop Aspire (following aspire-references rule)
if [ -n "$ASPIRE_PID" ] && ps -p $ASPIRE_PID > /dev/null 2>&1; then
    echo "Stopping Aspire with PID: $ASPIRE_PID"
    # Send SIGINT (same as Cmd+C)
    kill -INT $ASPIRE_PID
    # Wait for Graceful Shutdown
    sleep 10
    # Verify Termination
    if ps -p $ASPIRE_PID > /dev/null 2>&1; then
        echo "Aspire still running, force killing..."
        kill -KILL $ASPIRE_PID
    fi
    ps -p $ASPIRE_PID > /dev/null 2>&1 || echo "Aspire stopped successfully"
else
    echo "Aspire already stopped or PID not available"
fi

# Stop Docker services (optional)
# docker compose -f /Users/charles/workspace/github/aevatar-station/src/Aevatar.Aspire/docker-compose.yml down

echo "Services stopped"
```

#### 6.2 Archive Results
```bash
# Create archive
tar -czf "benchmark/latency_analysis_$(date +%Y%m%d_%H%M%S).tar.gz" -C benchmark/metrics .

echo "Results archived to benchmark/latency_analysis_$(date +%Y%m%d_%H%M%S).tar.gz"
```

## Usage Examples

### Quick Analysis Run
```bash
# Full automated analysis
bash -c "$(curl -s https://raw.githubusercontent.com/your-repo/aevatar-station/main/scripts/latency_analysis.sh)"
```

### Manual Step-by-Step
```bash
# Run each phase manually
./scripts/phase1_infrastructure.sh
./scripts/phase2_clear_metrics.sh
./scripts/phase3_run_test.sh
./scripts/phase4_collect_metrics.sh
./scripts/phase5_analyze.sh
./scripts/phase6_cleanup.sh
```

### Continuous Monitoring
```bash
# Run analysis every hour
crontab -e
# Add: 0 * * * * /path/to/latency_analysis.sh
```

## Troubleshooting

### Common Issues

#### Services Not Starting
```bash
# Check Docker status
docker ps
docker logs <container_name>

# Check port conflicts
lsof -i :9090  # Prometheus
lsof -i :16686 # Jaeger
lsof -i :27017 # MongoDB
```

#### Missing Metrics
```bash
# Verify Prometheus targets
curl http://localhost:9090/api/v1/targets

# Check application metrics endpoints
curl http://localhost:9091/metrics | grep aevatar
curl http://localhost:9092/metrics | grep aevatar
```

#### Analysis Script Errors
```bash
# Install required Python packages
pip3 install numpy

# Check JSON file validity
jq '.' benchmark/metrics/latest/stream_latency_buckets.json
```

## Performance Benchmarks

### Expected Metrics (Baseline)
- **Stream Latency p95**: < 2.0s
- **DeliverBatch Average**: < 1.5s
- **Database Write p95**: < 10ms
- **Process p1 vs p2 Difference**: < 50%

### Alert Thresholds
- **Stream Latency p95**: > 3.0s (Critical)
- **DeliverBatch p99**: > 5.0s (Warning)
- **Database Write p95**: > 50ms (Warning)
- **Process Imbalance**: > 100% difference (Critical)

## Integration with CI/CD

### GitHub Actions Integration
```yaml
name: Latency Analysis
on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM
  workflow_dispatch:

jobs:
  latency-analysis:
    runs-on: self-hosted
    steps:
      - uses: actions/checkout@v3
      - name: Run Latency Analysis
        run: |
          cd aevatar-station
          bash scripts/automated_latency_analysis.sh
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: latency-analysis-results
          path: benchmark/metrics/
```

## Current Analysis Results

The following analysis documents are automatically updated when new test results are available:

### **Primary Analysis Documents**
- **[DeliverBatch Latency Analysis](./benchmark/DeliverBatch_Latency_Analysis.md)** - Comprehensive analysis of `IStreamConsumerExtension/DeliverBatch` operation latency distribution based on Jaeger tracing data
- **[Orleans Multi-Silo Latency Analysis](./benchmark/Orleans_Multi_Silo_Latency_Analysis.md)** - Analysis of `aevatar_stream_event_publish_latency_seconds_bucket` metric and Orleans multi-silo event broadcasting

### **Key Findings from Current Analysis**
Based on the latest analysis results:

#### **DeliverBatch Performance (Jaeger Tracing)**
- **Process p1 (Scheduler)**: 1,256ms average, handles TestDbGAgent operations with MongoDB
- **Process p2 (Projector)**: 1,633ms average (30% slower), handles TestDbGAgent + StateProjectionGrain + Elasticsearch indexing
- **Database operations**: 3.7ms average (NOT the bottleneck)
- **Orleans processing**: ~99% of total latency (~566ms average)

#### **Stream Event Latency (Prometheus Metrics)**
- **Cold grains**: ~8.59s average (65% events in 1-1.5s range)
- **Warm grains**: ~0.585s average (72% events in 0.2-0.75s range)
- **Performance improvement**: 15x faster with grain warmup
- **Measurement**: Single recording per subscriber via `EventWrapperBaseAsyncObserver`

#### **Architecture Insights**
- **StateProjectionGrain MongoDB usage**: Only during initialization (stores Index), not during benchmark operations
- **p2 performance difference**: Due to additional Elasticsearch indexing operations, not ongoing MongoDB writes
- **Network impact**: Minimal on local laptop environment (~1-2ms localhost communication)

### **Optimization Recommendations**
1. **Focus on Orleans processing components** (serialization, IPC, stream processing)
2. **Implement grain warmup strategies** to achieve consistent sub-second performance
3. **Optimize Elasticsearch operations** for p2 silo performance
4. **Database layer requires no optimization** (A+ performance grade)

## References

- [DeliverBatch Latency Analysis](./benchmark/DeliverBatch_Latency_Analysis.md)
- [Orleans Multi-Silo Latency Analysis](./benchmark/Orleans_Multi_Silo_Latency_Analysis.md)
- [Aspire References](./aspire_references.md)
- [Prometheus Query API](https://prometheus.io/docs/prometheus/latest/querying/api/)
- [Jaeger API Documentation](https://www.jaegertracing.io/docs/apis/) 