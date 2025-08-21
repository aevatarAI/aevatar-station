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

# Common function: Process benchmark results with jq
process_results() {
    local result_file="$1"
    local metrics_file="$2"
    local test_type="$3"
    
    if [ ! -f "$result_file" ]; then
        echo "❌ Result file $result_file not found"
        echo "status=ERROR" > benchmark_status.txt
        return 1
    fi
    
    echo "📊 Processing $test_type benchmark results..."
    
    case $test_type in
        "BroadcastLatency")
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
                  }' "$result_file" > "$metrics_file"
            ;;
            
        "Latency")
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
                  }' "$result_file" > "$metrics_file"
            ;;
    esac
}

# Common function: Check thresholds and generate status
check_thresholds() {
    local test_type="$1"
    local metrics_file="$2"
    
    case $test_type in
        "BroadcastLatency")
            local success_rate=$(jq -r '.successRate' "$metrics_file")
            local p95=$(jq -r '.p95' "$metrics_file")
            local threshold_success_rate=${BCAST_SUCCESS_RATE:-"0.90"}
            local threshold_p95=${BCAST_P95_MS:-"300"}
            
            echo "📊 Broadcast Benchmark Results:"
            printf "   ✅ Success Rate: %.1f%% (target: ≥%.1f%%)\n" $(echo "$success_rate * 100" | bc -l) $(echo "$threshold_success_rate * 100" | bc -l)
            printf "   📈 P95 Latency: %.1fms (target: ≤%sms)\n" $p95 $threshold_p95
            
            # Check thresholds
            local fail=0
            awk -v a="$success_rate" -v b="$threshold_success_rate" 'BEGIN{ if (a+0 < b+0) exit 1 }' || fail=1
            awk -v a="$p95" -v b="$threshold_p95" 'BEGIN{ if (a+0 > b+0) exit 1 }' || fail=1
            ;;
            
        "Latency")
            local success=$(jq -r '.success' "$metrics_file")
            local p95=$(jq -r '.p95' "$metrics_file")
            local p99=$(jq -r '.p99' "$metrics_file")
            local processed_ratio=$(jq -r '.processedRatio' "$metrics_file")
            local threshold_p95=${LAT_P95_MS:-"300"}
            local threshold_p99=${LAT_P99_MS:-"3000"}
            local threshold_processed=${LAT_PROCESSED_RATIO:-"0.90"}
            
            echo "📊 Latency Benchmark Results:"
            echo "   ✅ Execution Status: $success"
            printf "   📈 P95 Latency: %.1fms (target: ≤%sms)\n" $p95 $threshold_p95
            printf "   📊 P99 Latency: %.1fms (target: ≤%sms)\n" $p99 $threshold_p99
            printf "   🎯 Processed Ratio: %.1f%% (target: ≥%.1f%%)\n" $(echo "$processed_ratio * 100" | bc -l) $(echo "$threshold_processed * 100" | bc -l)
            
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
            
            # Process results
            process_results "broadcast-latency-results.json" "broadcast_metrics.json" "BroadcastLatency"
            check_thresholds "BroadcastLatency" "broadcast_metrics.json"
            
            # Output complete JSON to logs for reliable collection
            echo "📄 === BROADCAST_JSON_BEGIN ==="
            cat broadcast-latency-results.json
            echo "📄 === BROADCAST_JSON_END ==="
            
            echo "📊 === BROADCAST_METRICS_BEGIN ==="
            cat broadcast_metrics.json
            echo "📊 === BROADCAST_METRICS_END ==="
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
            
            # Process results
            process_results "latency-results.json" "latency_metrics.json" "Latency"
            check_thresholds "Latency" "latency_metrics.json"
            
            # Output complete JSON to logs for reliable collection
            echo "📄 === LATENCY_JSON_BEGIN ==="
            cat latency-results.json
            echo "📄 === LATENCY_JSON_END ==="
            
            echo "📊 === LATENCY_METRICS_BEGIN ==="
            cat latency_metrics.json
            echo "📊 === LATENCY_METRICS_END ==="
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