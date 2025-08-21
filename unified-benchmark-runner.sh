#!/bin/bash
set -e

# Unified Benchmark Runner for Ephemeral Environment
# Supports multiple benchmark types via BENCHMARK_TYPE environment variable

BENCHMARK_TYPE=${BENCHMARK_TYPE:-"broadcast"}

echo "🚀 Starting Unified Benchmark Runner in Ephemeral Environment"
echo "📋 Benchmark Type: $BENCHMARK_TYPE"

# CLIENT_ID is required for ephemeral environment configuration
if [[ -z "$CLIENT_ID" ]]; then
    echo "❌ ERROR: CLIENT_ID environment variable is required!"
    echo "   This should be provided by the ephemeral environment setup."
    exit 1
fi

# Common Orleans connection configuration
export Orleans__MongoDBClient="mongodb://env-${CLIENT_ID}-mongo:27017/AevatarDb"
export Orleans__ClusterId="${CLIENT_ID}SiloCluster"
export Orleans__ServiceId="${CLIENT_ID}BasicService"
export Orleans__DataBase="AevatarDb"
export Orleans__HostId="${CLIENT_ID}"

# Kafka broker configuration for ephemeral environment
export KAFKA_BROKERS="env-${CLIENT_ID}-kafka-kafka-bootstrap:9092"

echo "🔗 Orleans Connection:"
echo "  MongoDB: ${Orleans__MongoDBClient}"
echo "  Cluster ID: ${Orleans__ClusterId}"
echo "  Service ID: ${Orleans__ServiceId}"
echo "  Database: ${Orleans__DataBase}"
echo "  Host ID: ${Orleans__HostId}"
echo "  Discovery: MongoDB Clustering (Auto-Discovery)"
echo "📡 Kafka Connection:"
echo "  Brokers: ${KAFKA_BROKERS}"

# Orleans client will handle connection automatically via MongoDB clustering

# Removed process_results function - thresholds now checked directly from results.json

# Parse and check thresholds - adapted from performance-benchmarks.yml
parse_and_check_thresholds() {
    local test_type="$1"
    local results_file="$2"
    
    case $test_type in
        "BroadcastLatency")
            echo "📊 Processing broadcast benchmark results..."
            
            # Extract metrics using the proven jq logic from performance-benchmarks.yml
            jq -r '
                .Results[0] as $r
                | (($r.TotalEventsSent * ($r.SubscriberCount // 0))) as $expected
                | (if ($r.ActualDurationSeconds // 0) > 0 then ($r.TotalEventsProcessed / $r.ActualDurationSeconds) else 0 end) as $throughput
                | (if $expected > 0 then ($r.TotalEventsProcessed / $expected) else 0 end) as $successRate
                | {
                    successRate: $successRate,
                    throughput: $throughput,
                    avg: ($r.AverageLatencyMs // 0),
                    p95: ($r.P95LatencyMs // 0),
                    testType: "BroadcastLatency"
                  }' "$results_file" > broadcast_metrics.json
            
            # Extract values for display and threshold checking
            local success_rate=$(jq -r '.successRate' broadcast_metrics.json)
            local p95=$(jq -r '.p95' broadcast_metrics.json)
            local avg=$(jq -r '.avg' broadcast_metrics.json)
            local throughput=$(jq -r '.throughput' broadcast_metrics.json)
            local threshold_success_rate=${BCAST_SUCCESS_RATE:-"0.90"}
            local threshold_p95=${BCAST_P95_MS:-"120"}
            
            echo "📊 Broadcast Benchmark Results:"
            printf "   ✅ Success Rate: %.1f%% (target: ≥%.1f%%)\n" $(echo "$success_rate * 100" | bc -l) $(echo "$threshold_success_rate * 100" | bc -l)
            printf "   📈 P95 Latency: %.1fms (target: ≤%sms)\n" $p95 $threshold_p95
            printf "   ⏱️  Avg Latency: %.1fms\n" $avg  
            printf "   ⚡ Throughput: %.1f events/sec\n" $throughput
            
            # Output compact metrics JSON to logs
            echo "📊 === BROADCAST_METRICS_BEGIN ==="
            cat broadcast_metrics.json
            echo "📊 === BROADCAST_METRICS_END ==="
            
            # Check thresholds
            local fail=0
            awk -v a="$success_rate" -v b="$threshold_success_rate" 'BEGIN{ if (a+0 < b+0) exit 1 }' || fail=1
            awk -v a="$p95" -v b="$threshold_p95" 'BEGIN{ if (a+0 > b+0) exit 1 }' || fail=1
            ;;
            
        "Latency")
            echo "📊 Processing latency benchmark results..."
            
            # Extract metrics using the proven jq logic from performance-benchmarks.yml
            jq -r '
                .Results | max_by(.ConcurrencyLevel) as $r
                | (if ($r.TotalEventsSent // 0) > 0 then ($r.TotalEventsProcessed / $r.TotalEventsSent) else 0 end) as $processedRatio
                | {
                    success: ($r.Success // false),
                    throughput: ($r.ActualThroughput // 0),
                    p95: ($r.P95LatencyMs // 0),
                    p99: ($r.P99LatencyMs // 0),
                    processedRatio: $processedRatio,
                    testType: "Latency"
                  }' "$results_file" > latency_metrics.json
            
            # Extract values for display and threshold checking
            local success=$(jq -r '.success' latency_metrics.json)
            local p95=$(jq -r '.p95' latency_metrics.json)
            local p99=$(jq -r '.p99' latency_metrics.json)
            local throughput=$(jq -r '.throughput' latency_metrics.json)
            local processed_ratio=$(jq -r '.processedRatio' latency_metrics.json)
            local threshold_p95=${LAT_P95_MS:-"120"}
            local threshold_p99=${LAT_P99_MS:-"1000"}
            local threshold_processed=${LAT_PROCESSED_RATIO:-"1.0"}
            
            echo "📊 Latency Benchmark Results:"
            echo "   ✅ Execution Status: $success"
            printf "   📈 P95 Latency: %.1fms (target: ≤%sms)\n" $p95 $threshold_p95
            printf "   📊 P99 Latency: %.1fms (target: ≤%sms)\n" $p99 $threshold_p99
            printf "   🎯 Processed Ratio: %.1f%% (target: ≥%.1f%%)\n" $(echo "$processed_ratio * 100" | bc -l) $(echo "$threshold_processed * 100" | bc -l)
            printf "   ⚡ Throughput: %.1f events/sec\n" $throughput
            
            # Output compact metrics JSON to logs
            echo "📊 === LATENCY_METRICS_BEGIN ==="
            cat latency_metrics.json
            echo "📊 === LATENCY_METRICS_END ==="
            
            # Check thresholds
            local fail=0
            if [ "$success" != "true" ]; then fail=1; fi
            awk -v a="$p95" -v b="$threshold_p95" 'BEGIN{ if (a+0 > b+0) exit 1 }' || fail=1
            awk -v a="$p99" -v b="$threshold_p99" 'BEGIN{ if (a+0 > b+0) exit 1 }' || fail=1
            awk -v a="$processed_ratio" -v b="$threshold_processed" 'BEGIN{ if (a+0 < b+0) exit 1 }' || fail=1
            ;;
    esac
    
    echo "$fail" > threshold_result.txt
    
    if [ "$fail" -eq 0 ]; then
        echo "✅ $test_type benchmark PASSED all thresholds"
        echo "status=PASSED" > benchmark_status.txt
    else
        echo "❌ $test_type benchmark FAILED threshold checks"
        echo "status=FAILED" > benchmark_status.txt
    fi
}

# Removed size control function - now using compact metrics JSON approach from performance-benchmarks.yml

# Main execution logic
main() {
    # Create results directory
    mkdir -p /tmp/results
    cd /tmp/results
    
    # Orleans client will auto-connect via MongoDB clustering
    
    case $BENCHMARK_TYPE in
        "broadcast")
            echo "📡 Running Broadcast Latency Benchmark..."
            
            # Broadcast benchmark parameters with defaults
            BCAST_SUBS=${BCAST_SUBS:-"128"}
            BCAST_PUBS=${BCAST_PUBS:-"1"}
            BCAST_EPS=${BCAST_EPS:-"1"}
            BCAST_DURATION=${BCAST_DURATION:-"120"}
            BCAST_WARMUP=${BCAST_WARMUP:-"5"}
            
            echo "  Parameters: ${BCAST_SUBS} subscribers, ${BCAST_PUBS} publishers, ${BCAST_DURATION}s duration"
            
                    # Run broadcast benchmark
        dotnet /app/BroadcastLatencyBenchmark/BroadcastLatencyBenchmark.dll \
                --subscriber-count ${BCAST_SUBS} \
                --publisher-count ${BCAST_PUBS} \
                --events-per-second ${BCAST_EPS} \
                --duration ${BCAST_DURATION} \
                --warmup-duration ${BCAST_WARMUP} \
                --output-file broadcast-latency-results.json
            
            # Parse and check thresholds using proven performance-benchmarks.yml logic
            parse_and_check_thresholds "BroadcastLatency" "broadcast-latency-results.json"
            ;;
            
        "latency")
            echo "⚡ Running Latency Benchmark..."
            
            # Latency benchmark parameters with defaults
            LAT_MAX_CONCURRENCY=${LAT_MAX_CONCURRENCY:-"16"}
            LAT_DURATION=${LAT_DURATION:-"120"}
            LAT_WARMUP=${LAT_WARMUP:-"5"}
            LAT_EPS=${LAT_EPS:-"1"}
            
            echo "  Parameters: ${LAT_MAX_CONCURRENCY} max concurrency, ${LAT_DURATION}s duration"
            
                    # Run latency benchmark
        dotnet /app/LatencyBenchmark/LatencyBenchmark.dll \
                --max-concurrency ${LAT_MAX_CONCURRENCY} \
                --events-per-second ${LAT_EPS} \
                --duration ${LAT_DURATION} \
                --warmup-duration ${LAT_WARMUP} \
                --output-file latency-results.json
            
            # Parse and check thresholds using proven performance-benchmarks.yml logic
            parse_and_check_thresholds "Latency" "latency-results.json"
            ;;
            
        *)
            echo "❌ Unknown benchmark type: $BENCHMARK_TYPE"
            echo "   Supported types: broadcast, latency"
            echo "status=ERROR" > benchmark_status.txt
            exit 1
            ;;
    esac
    
    # Create summary
    local status=$(cat benchmark_status.txt | cut -d'=' -f2)
    cat > benchmark_summary.txt << EOF
🎯 $BENCHMARK_TYPE Benchmark Summary:
- Test Type: Orleans $(echo $BENCHMARK_TYPE | sed 's/broadcast/Broadcast Messaging/g' | sed 's/latency/Point-to-Point Messaging/g')
- Status: $status
- Results available in: /tmp/results/
EOF
    
    cat benchmark_summary.txt
    
    echo "🎉 $BENCHMARK_TYPE Benchmark completed"
    echo "📁 Results available in /tmp/results/"
    
    # Results are ready, let container exit so K8s Job can complete
    echo "📁 Results ready for collection in /tmp/results/"
    echo "🎉 Container will now exit to complete K8s Job"
    echo "✅ Benchmark execution finished successfully"
}

# Execute main function
main "$@"